using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using DuskOfCoding.Domain.Interfaces;
using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Infrastructure.Configuration;
using DuskOfCoding.Infrastructure.Messaging;
using DuskOfCoding.TutorWorker.Prompts;
using DuskOfCoding.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace DuskOfCoding.TutorWorker;

/// <summary>
/// Background service that consumes GenerateTestSuiteCommands from RabbitMQ,
/// calls the LLM to generate xUnit test code, saves the results as TaskTest entities,
/// and notifies the originating user via a SignalR result message.
/// </summary>
public sealed class TestGenerationWorkerService : BackgroundService
{
    private readonly RabbitMQService _rabbitMqService;
    private readonly IChatClient _chatClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LlmProviderOptions _options;
    private readonly ILogger<TestGenerationWorkerService> _logger;

    public TestGenerationWorkerService(
        RabbitMQService rabbitMqService,
        IChatClient chatClient,
        IServiceScopeFactory scopeFactory,
        IOptions<LlmProviderOptions> options,
        ILogger<TestGenerationWorkerService> logger)
    {
        _rabbitMqService = rabbitMqService;
        _chatClient = chatClient;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TestGenerationWorkerService starting, subscribing to {Queue}...",
            RabbitMQTopology.TestGenerationQueue);

        var channel = await _rabbitMqService.ConsumeAsync<GenerateTestSuiteCommand>(
            RabbitMQTopology.TestGenerationQueue,
            HandleCommandAsync,
            stoppingToken);

        _logger.LogInformation("TestGenerationWorkerService is now consuming commands");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TestGenerationWorkerService shutting down...");
        }
        finally
        {
            await channel.DisposeAsync();
        }
    }

    private async Task HandleCommandAsync(GenerateTestSuiteCommand command, Guid correlationId, CancellationToken ct)
    {
        _logger.LogInformation(
            "Processing test generation for Task {TaskId} requested by User {UserId}",
            command.TaskId, command.UserId);

        bool isSuccess = false;
        string messageKey = "Test_Suite_Error_Key";

        try
        {
            // 1. Call LLM — using data from the command, no DB tracking yet
            var generatedTests = await InvokeLlmAsync(command, ct);

            // 2. Direct database update (Disconnected Approach)
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Phase 22.2: Bulk delete existing tests and insert new ones.
            // This avoids DbUpdateConcurrencyException because we never load/track the Task entity itself.
            await db.TaskTests
                .Where(t => t.TaskDefinitionId == command.TaskId)
                .ExecuteDeleteAsync(ct);

            var newTests = generatedTests.Select(g => new TaskTest
            {
                TaskDefinitionId = command.TaskId,
                Name = g.Name,
                Code = g.Code
            }).ToList();

            db.TaskTests.AddRange(newTests);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Successfully generated and saved {Count} tests for Task {TaskId}",
                newTests.Count, command.TaskId);

            isSuccess = true;
            messageKey = "Test_Suite_Success_Key";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate or save tests for Task {TaskId}", command.TaskId);
            messageKey = "Test_Suite_Error_Key";
        }
        finally
        {
            // 3. Publish result so the Bridge can push it to SignalR
            await _rabbitMqService.PublishAsync(
                RabbitMQTopology.ResponseExchange,
                RabbitMQTopology.TestGenerationResponseRoutingKey,
                new TestGenerationResultMessage
                {
                    TaskId = command.TaskId,
                    UserId = command.UserId,
                    IsSuccess = isSuccess,
                    MessageKey = messageKey
                },
                correlationId,
                ct);
        }
    }

    private async Task<List<(string Name, string Code)>> InvokeLlmAsync(GenerateTestSuiteCommand command, CancellationToken ct)
    {
        var instruction = GenerateTestsPrompt.GetInstruction(command.ExpectedClassName, command.Namespace);
        var userContent = $"{instruction}\n\nTask Title: {command.Title}\nDescription: {command.Description}";

        using var scope = _scopeFactory.CreateScope();
        var kernel = scope.ServiceProvider.GetRequiredService<Microsoft.SemanticKernel.Kernel>();
        kernel.Plugins.AddFromType<McpTools.ExecuteCustomTestTool>();

        var agent = new Microsoft.SemanticKernel.Agents.ChatCompletionAgent
        {
            Name = "TestGeneratorAgent",
            Instructions = instruction,
            Kernel = kernel
        };

        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
        chatHistory.AddUserMessage(userContent);

        var responseText = string.Empty;
        await foreach (var item in agent.InvokeAsync(chatHistory, cancellationToken: ct))
        {
            if (!string.IsNullOrWhiteSpace(item.Message.Content))
            {
                responseText += item.Message.Content;
            }
        }

        // --- LLM Observability Telemetry (Fire-and-Forget) ---
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var log = new LlmTelemetryLog
                {
                    TaskId = command.TaskId,
                    UserId = Guid.TryParse(command.UserId, out var uid) ? uid : (Guid?)null,
                    ModelName = _options.ModelId ?? "unknown",
                    TokenCount = 0,
                    IsSuccess = true,
                    Payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        SystemPrompt = instruction,
                        UserPrompt = userContent,
                        RawResponse = responseText
                    })
                };
                
                db.LlmTelemetryLogs.Add(log);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save LLM Telemetry Log");
            }
        });
        // -----------------------------------------------------

        // Robust markdown stripping — LLMs frequently ignore formatting instructions
        var text = responseText.Trim();
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            text = text[7..];
            if (text.EndsWith("```")) text = text[..^3];
        }
        else if (text.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            text = text[3..];
            if (text.EndsWith("```")) text = text[..^3];
        }

        text = text.Trim();

        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var parsed = System.Text.Json.JsonSerializer.Deserialize<List<LlmTestResult>>(text, options)
            ?? throw new InvalidOperationException("LLM returned an empty or null test list.");

        return parsed.Select(t => (t.Name, t.Code)).ToList();
    }

    /// <summary>Internal DTO matching the LLM JSON response shape.</summary>
    private sealed record LlmTestResult(string Name, string Code);
}
