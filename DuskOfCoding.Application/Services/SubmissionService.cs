using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Enums;
using DuskOfCoding.Domain.Interfaces;
using DuskOfCoding.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DuskOfCoding.Application.Services;

public class SubmissionService : ISubmissionService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(
        ITaskRepository taskRepository,
        ISubmissionRepository submissionRepository,
        ILogger<SubmissionService> logger)
    {
        _taskRepository = taskRepository;
        _submissionRepository = submissionRepository;
        _logger = logger;
    }

    public async Task<SubmissionResult> SubmitCodeAsync(Guid taskId, string sourceCode, Guid? userId = null, string? preferredLanguage = null, CancellationToken ct = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, ct);
        if (task == null)
            throw new ArgumentException("Task not found.");

        var submission = new Submission
        {
            TaskId = taskId,
            UserId = userId,
            SourceCode = sourceCode,
            PreferredLanguage = preferredLanguage,
            Status = SubmissionStatus.Pending
        };
        
        await _submissionRepository.AddAsync(submission, ct);

        _logger.LogInformation("Successfully created pending submission {SubmissionId} for task {TaskId} (userId: {UserId})", 
            submission.Id, taskId, userId);

        // Feedback is null because it will be generated asynchronously by the TutorWorker
        return new SubmissionResult(submission, null);
    }

    public async Task<SubmissionResult?> GetSubmissionByIdAsync(Guid id, CancellationToken ct = default)
    {
        var submission = await _submissionRepository.GetByIdAsync(id, ct);
        if (submission == null) return null;

        Feedback? feedback = null;
        if (submission.Feedback != null)
        {
            feedback = new Feedback
            {
                IsSuccess = submission.Feedback.IsSuccess,
                Summary = submission.Feedback.Summary,
                CompilationMessages = submission.Feedback.GetCompilationMessages(),
                TestMessages = submission.Feedback.GetTestMessages(),
                AiReviewRemarks = submission.Feedback.AiReviewRemarks
            };
        }
        
        return new SubmissionResult(submission, feedback);
    }
}
