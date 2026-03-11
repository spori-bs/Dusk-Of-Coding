using System.Net.Http.Json;
using PracticePlatform.Domain.Entities;
using PracticePlatform.Domain.Interfaces;
using PracticePlatform.Domain.Models;
using PracticePlatform.Domain.Enums;
using PracticePlatform.Execution.Contracts.DTOs;

namespace PracticePlatform.Infrastructure.Services;

public class HttpCodeExecutionEngine : ICodeExecutionEngine
{
    private readonly HttpClient _httpClient;

    public HttpCodeExecutionEngine(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ExecutionResult> ExecuteAsync(
        TaskDefinition task,
        Submission submission,
        CancellationToken ct = default)
    {
        var request = new ExecutionRequest
        {
            TaskId = task.Id,
            SubmissionId = submission.Id,
            SourceCode = submission.SourceCode,
            Language = nameof(ProgrammingLanguage.CSharp).ToLowerInvariant(), // Send to execution API as lower string
            TestBundle = new TestBundleDto
            {
                Framework = "xunit",
                ProjectTemplatePath = task.TestBundleReference
            },
            Limits = new LimitsDto()
        };

        var response = await _httpClient.PostAsJsonAsync("/api/executions", request, ct);
        response.EnsureSuccessStatusCode();

        var apiResult = await response.Content.ReadFromJsonAsync<ExecutionResponse>(cancellationToken: ct);

        if (apiResult == null)
        {
            return new ExecutionResult
            {
                CompilationSucceeded = false,
                RuntimeErrors = new[] { "Failed to deserialize execution response from API." }
            };
        }

        return new ExecutionResult
        {
            CompilationSucceeded = apiResult.Compilation?.Succeeded ?? false,
            CompilationErrors = apiResult.Compilation?.Errors ?? Array.Empty<string>(),
            RuntimeErrors = apiResult.Errors ?? Array.Empty<string>(),
            TotalDurationMs = apiResult.Runtime?.TotalDurationMs ?? 0,
            Tests = apiResult.Tests?.Select(t => new TestResult(
                t.Name, 
                t.Passed, 
                t.Message, 
                t.DurationMs)).ToArray() ?? Array.Empty<TestResult>()
        };
    }
}
