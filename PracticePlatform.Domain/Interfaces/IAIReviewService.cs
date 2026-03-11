using PracticePlatform.Domain.Entities;
using PracticePlatform.Domain.Models;

namespace PracticePlatform.Domain.Interfaces;

public interface IAIReviewService
{
    Task<Feedback> EnrichFeedbackAsync(
        TaskDefinition task,
        Submission submission,
        ExecutionResult executionResult,
        CancellationToken ct = default);
}
