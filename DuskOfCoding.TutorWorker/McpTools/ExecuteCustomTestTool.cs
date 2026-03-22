using System.ComponentModel;
using System.Net.Http.Json;
using ModelContextProtocol.Server;
using DuskOfCoding.Execution.Contracts.DTOs;

namespace DuskOfCoding.TutorWorker.McpTools;

/// <summary>
/// MCP tool that executes student code with custom tests via the Execution API.
/// Exposed to the LLM so it can run code and tests to validate student submissions.
/// </summary>
[McpServerToolType]
public static class ExecuteCustomTestTool
{
    private const string ExecutionApiClientName = "executionapi";
    private static readonly TimeSpan ToolTimeout = TimeSpan.FromSeconds(5);

    [McpServerTool(Name = "execute_custom_test"), Description("Compiles and runs student C# code along with custom test code using the sandboxed Execution API. Returns compilation results, test outcomes, and runtime metrics. Use this to verify if the student's code produces the correct output.")]
    public static async Task<string> ExecuteCustomTest(
        [Description("The student's C# source code to compile and test")] string studentCode,
        [Description("Optional custom test code (e.g. xUnit tests) to run against the student code. Leave empty to run default compilation only.")] string? testCode,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ToolTimeout);
        var token = cts.Token;

        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(ExecutionApiClientName);

        var request = new ExecutionRequest
        {
            TaskId = Guid.Empty,
            SubmissionId = Guid.NewGuid(),
            SourceCode = studentCode,
            Language = "csharp",
            TestBundle = testCode is not null
                ? new TestBundleDto { Framework = "xunit", TestCode = "inline" }
                : null,
            Limits = new LimitsDto
            {
                CpuSeconds = 5,
                MemoryMb = 256,
                WallClockSeconds = 10
            }
        };

        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/executions", request, token);
            response.EnsureSuccessStatusCode();

            var executionResult = await response.Content.ReadFromJsonAsync<ExecutionResponse>(cancellationToken: token);

            if (executionResult is null)
                return "❌ Failed to parse execution response.";

            var lines = new List<string>
            {
                "## Execution Result",
                $"**Status**: {executionResult.Status}",
                $"**Compilation**: {(executionResult.Compilation?.Succeeded == true ? "✅ Succeeded" : "❌ Failed")}",
            };

            if (executionResult.Compilation?.Errors?.Count > 0)
            {
                lines.Add("**Compilation Errors**:");
                foreach (var error in executionResult.Compilation.Errors)
                    lines.Add($"  - {error}");
            }

            if (executionResult.Tests?.Count > 0)
            {
                lines.Add($"**Tests**: {executionResult.Tests.Count(t => t.Passed)} / {executionResult.Tests.Count} passed");
                foreach (var test in executionResult.Tests)
                {
                    var icon = test.Passed ? "✅" : "❌";
                    lines.Add($"  {icon} {test.Name} ({test.DurationMs}ms)");
                    if (!string.IsNullOrEmpty(test.Message))
                        lines.Add($"     Message: {test.Message}");
                }
            }

            if (executionResult.Runtime is not null)
                lines.Add($"**Total Duration**: {executionResult.Runtime.TotalDurationMs}ms");

            return string.Join("\n", lines);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return "⏱️ Execution timed out after 5 seconds. The Execution API may be unresponsive.";
        }
        catch (Exception ex)
        {
            return $"❌ Execution failed: {ex.Message}";
        }
    }
}
