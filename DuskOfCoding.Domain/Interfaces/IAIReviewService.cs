using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Models;

namespace DuskOfCoding.Domain.Interfaces;

public interface IAIReviewService
{
    Task<Feedback> EnrichFeedbackAsync(
        TaskDefinition task,
        Submission submission,
        ExecutionResult executionResult,
        CancellationToken ct = default);
}
