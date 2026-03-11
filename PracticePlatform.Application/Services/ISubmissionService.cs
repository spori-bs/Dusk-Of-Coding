using PracticePlatform.Domain.Entities;
using PracticePlatform.Domain.Models;

namespace PracticePlatform.Application.Services;

public record SubmissionResult(Submission Submission, Feedback? Feedback);

public interface ISubmissionService
{
    Task<SubmissionResult> SubmitCodeAsync(Guid taskId, string sourceCode, Guid? userId = null, CancellationToken ct = default);
    Task<SubmissionResult?> GetSubmissionByIdAsync(Guid id, CancellationToken ct = default);
}
