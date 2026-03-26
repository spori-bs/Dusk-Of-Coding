using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Interfaces;
using DuskOfCoding.Domain.Models;

namespace DuskOfCoding.Infrastructure.Services;

public class NoOpAIReviewService : IAIReviewService
{
    public Task<Feedback> EnrichFeedbackAsync(
        TaskDefinition task, 
        Submission submission, 
        ExecutionResult executionResult, 
        string? preferredLanguage = null,
        CancellationToken ct = default)
    {
        // Return simple feedback based solely on execution result
        return Task.FromResult(new Feedback
        {
            IsSuccess = executionResult.CompilationSucceeded && executionResult.Tests.All(t => t.Passed),
            Summary = executionResult.CompilationSucceeded ? "Execution completed." : "Compilation failed.",
            CompilationMessages = executionResult.CompilationErrors,
            TestMessages = executionResult.Tests.Select(t => $"{t.Name}: {(t.Passed ? "Passed" : "Failed")} {t.Message}").ToList(),
            AiReviewRemarks = "AI review skipped for POC."
        });
    }
}
