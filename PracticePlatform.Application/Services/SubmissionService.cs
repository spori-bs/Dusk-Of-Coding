using System.Collections.Concurrent;
using PracticePlatform.Domain.Entities;
using PracticePlatform.Domain.Interfaces;
using PracticePlatform.Domain.Models;

namespace PracticePlatform.Application.Services;

public class SubmissionService : ISubmissionService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly ICodeExecutionEngine _executionEngine;
    private readonly IAIReviewService _aiReviewService;

    // Temporary storage for Feedback since our POC domain models didn't link Feedback directly 
    // to Submission in the DB. In Phase 7 (EFCore) we'll persist this properly.
    private static readonly ConcurrentDictionary<Guid, Feedback> _feedbacks = new();

    public SubmissionService(
        ITaskRepository taskRepository,
        ISubmissionRepository submissionRepository,
        ICodeExecutionEngine executionEngine,
        IAIReviewService aiReviewService)
    {
        _taskRepository = taskRepository;
        _submissionRepository = submissionRepository;
        _executionEngine = executionEngine;
        _aiReviewService = aiReviewService;
    }

    public async Task<SubmissionResult> SubmitCodeAsync(Guid taskId, string sourceCode, Guid? userId = null, CancellationToken ct = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, ct);
        if (task == null)
            throw new ArgumentException("Task not found.");

        var submission = new Submission
        {
            TaskId = taskId,
            UserId = userId,
            SourceCode = sourceCode,
            Status = "Pending"
        };
        await _submissionRepository.AddAsync(submission, ct);

        try
        {
            submission.Status = "Executing";
            await _submissionRepository.UpdateAsync(submission, ct);

            var executionResult = await _executionEngine.ExecuteAsync(task, submission, ct);

            var feedback = await _aiReviewService.EnrichFeedbackAsync(task, submission, executionResult, ct);

            submission.Status = feedback.IsSuccess ? "Success" : "Failed";
            submission.CompletedAt = DateTime.UtcNow;
            await _submissionRepository.UpdateAsync(submission, ct);

            _feedbacks[submission.Id] = feedback;

            return new SubmissionResult(submission, feedback);
        }
        catch (Exception ex)
        {
            submission.Status = "Error";
            submission.CompletedAt = DateTime.UtcNow;
            await _submissionRepository.UpdateAsync(submission, ct);

            var errorFeedback = new Feedback 
            { 
                IsSuccess = false, 
                Summary = "Internal Execution Error", 
                AiReviewRemarks = ex.Message 
            };
            _feedbacks[submission.Id] = errorFeedback;

            return new SubmissionResult(submission, errorFeedback);
        }
    }

    public async Task<SubmissionResult?> GetSubmissionByIdAsync(Guid id, CancellationToken ct = default)
    {
        var submission = await _submissionRepository.GetByIdAsync(id, ct);
        if (submission == null) return null;

        _feedbacks.TryGetValue(id, out var feedback);
        return new SubmissionResult(submission, feedback);
    }
}
