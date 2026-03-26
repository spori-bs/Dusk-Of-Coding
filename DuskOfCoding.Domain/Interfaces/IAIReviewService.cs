using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Models;

namespace DuskOfCoding.Domain.Interfaces;

public interface IAIReviewService
{
    Task<Feedback> EnrichFeedbackAsync(
        TaskDefinition task,
        Submission submission,
        ExecutionResult executionResult,
        string? preferredLanguage = null,
        CancellationToken ct = default);
}
