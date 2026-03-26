using System.Collections.Concurrent;
using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Enums;
using DuskOfCoding.Domain.Interfaces;
using DuskOfCoding.Domain.Models;
using Microsoft.Extensions.DependencyInjection; // For GetRequiredKeyedService
using Microsoft.Extensions.Logging; // For ILogger
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DuskOfCoding.Application.Services;

public class SubmissionService : ISubmissionService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IAIReviewService _aiReviewService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(
        ITaskRepository taskRepository,
        ISubmissionRepository submissionRepository,
        IAIReviewService aiReviewService,
        IServiceProvider serviceProvider,
        ILogger<SubmissionService> logger)
    {
        _taskRepository = taskRepository;
        _submissionRepository = submissionRepository;
        _aiReviewService = aiReviewService;
        _serviceProvider = serviceProvider;
        _logger = logger;
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
            Status = SubmissionStatus.Pending
        };
        await _submissionRepository.AddAsync(submission, ct);

        try
        {
            submission.Status = SubmissionStatus.Executing;
            await _submissionRepository.UpdateAsync(submission, ct);

            // For Phase 12, we map incoming requests to the C# execution engine unconditionally for now,
            // but the architecture natively supports looking this up via the language enum mapped from the DTO.
            var targetLanguage = nameof(DuskOfCoding.Domain.Enums.ProgrammingLanguage.CSharp);

            // -- Roslyn Pre-Execution Syntax Check --
            if (targetLanguage == nameof(DuskOfCoding.Domain.Enums.ProgrammingLanguage.CSharp))
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var diagnostics = syntaxTree.GetDiagnostics()
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => $"Line {d.Location.GetLineSpan().StartLinePosition.Line + 1}: {d.GetMessage()}")
                    .ToList();

                if (diagnostics.Any())
                {
                    _logger.LogWarning("Syntax errors detected for submission {SubmissionId}", submission.Id);
                    submission.Status = SubmissionStatus.CompilationFailed;
                    submission.CompletedAt = DateTime.UtcNow;
                    await _submissionRepository.UpdateAsync(submission, ct);

                    var syntaxFeedback = new Feedback
                    {
                        IsSuccess = false,
                        Summary = "Compilation Failed",
                        CompilationMessages = diagnostics,
                        AiReviewRemarks = "The submission contains syntax errors. Please fix them before attempting execution."
                    };
                    
                    submission.Feedback ??= new FeedbackRecord { SubmissionId = submission.Id };
                    submission.Feedback.IsSuccess = false;
                    submission.Feedback.Summary = syntaxFeedback.Summary;
                    submission.Feedback.AiReviewRemarks = syntaxFeedback.AiReviewRemarks;
                    submission.Feedback.SetCompilationMessages(diagnostics);
                    
                    await _submissionRepository.UpdateAsync(submission, ct);
                    return new SubmissionResult(submission, syntaxFeedback);
                }
            }
            // ----------------------------------------

            _logger.LogInformation("Resolving execution engine for language {Language}", targetLanguage);
            var executionEngine = _serviceProvider.GetRequiredKeyedService<ICodeExecutionEngine>(targetLanguage);

            // 3. Execute code safely using the resolved language-specific engine
            _logger.LogInformation("Sending submission {SubmissionId} to execution engine", submission.Id);
            var executionResult = await executionEngine.ExecuteAsync(task, submission, ct);

            var feedback = await _aiReviewService.EnrichFeedbackAsync(task, submission, executionResult, ct);

            submission.Feedback ??= new FeedbackRecord { SubmissionId = submission.Id };
            submission.Feedback.IsSuccess = feedback.IsSuccess;
            submission.Feedback.Summary = feedback.Summary;
            submission.Feedback.AiReviewRemarks = feedback.AiReviewRemarks;
            submission.Feedback.SetCompilationMessages(feedback.CompilationMessages?.ToList() ?? new List<string>());
            submission.Feedback.SetTestMessages(feedback.TestMessages?.ToList() ?? new List<string>());

            submission.Status = feedback.IsSuccess ? SubmissionStatus.Success : SubmissionStatus.TestsFailed;
            submission.CompletedAt = DateTime.UtcNow;
            await _submissionRepository.UpdateAsync(submission, ct);

            return new SubmissionResult(submission, feedback);
        }
        catch (Exception ex)
        {
            submission.Feedback ??= new FeedbackRecord { SubmissionId = submission.Id };
            submission.Feedback.IsSuccess = false;
            submission.Feedback.Summary = "Internal Execution Error";
            submission.Feedback.AiReviewRemarks = ex.Message;

            submission.Status = SubmissionStatus.Error;
            submission.CompletedAt = DateTime.UtcNow;
            await _submissionRepository.UpdateAsync(submission, ct);

            return new SubmissionResult(submission, new Feedback { IsSuccess = false, Summary = "Internal Execution Error", AiReviewRemarks = ex.Message });
        }
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
