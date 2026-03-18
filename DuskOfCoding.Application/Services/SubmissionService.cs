using System.Collections.Concurrent;
using DuskOfCoding.Domain.Entities;
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

    // Temporary storage for Feedback since our POC domain models didn't link Feedback directly 
    // to Submission in the DB. In Phase 7 (EFCore) we'll persist this properly.
    private static readonly ConcurrentDictionary<Guid, Feedback> _feedbacks = new();

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
            Status = "Pending"
        };
        await _submissionRepository.AddAsync(submission, ct);

        try
        {
            submission.Status = "Executing";
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
                    submission.Status = "Failed";
                    submission.CompletedAt = DateTime.UtcNow;
                    await _submissionRepository.UpdateAsync(submission, ct);

                    var syntaxFeedback = new Feedback
                    {
                        IsSuccess = false,
                        Summary = "Compilation Failed",
                        CompilationMessages = diagnostics,
                        AiReviewRemarks = "The submission contains syntax errors. Please fix them before attempting execution."
                    };
                    _feedbacks[submission.Id] = syntaxFeedback;
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
