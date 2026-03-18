using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Interfaces;
using DuskOfCoding.Domain.Models;

namespace DuskOfCoding.Infrastructure.Services;

public class MockAIReviewService : IAIReviewService
{
    public async Task<Feedback> EnrichFeedbackAsync(
        TaskDefinition task, 
        Submission submission, 
        ExecutionResult executionResult, 
        CancellationToken ct = default)
    {
        // Simulate AI processing delay
        await Task.Delay(500, ct);

        bool isSuccess = executionResult.CompilationSucceeded && executionResult.Tests.All(t => t.Passed);

        string aiRemarks;
        if (!executionResult.CompilationSucceeded)
        {
            aiRemarks = "🤖 [AI Review]: It looks like your code has compilation errors. Check your syntax, missing semicolons, or undefined variables.";
        }
        else if (!isSuccess)
        {
            aiRemarks = "🤖 [AI Review]: Your code compiled successfully, but failed some tests. Consider edge cases that might not be handled correctly in your logic.";
        }
        else
        {
            aiRemarks = "🤖 [AI Review]: Great job! Your code is correct and passes all tests. The logic is clean, and variable names are quite readable. For further improvement, consider if there are ways to optimize the time or space complexity, though for this task, the current approach is perfectly fine.";
        }

        return new Feedback
        {
            IsSuccess = isSuccess,
            Summary = executionResult.CompilationSucceeded ? "Execution completed." : "Compilation failed.",
            CompilationMessages = executionResult.CompilationErrors,
            TestMessages = executionResult.Tests.Select(t => $"{t.Name}: {(t.Passed ? "Passed" : "Failed")} {t.Message}").ToList(),
            AiReviewRemarks = aiRemarks
        };
    }
}
