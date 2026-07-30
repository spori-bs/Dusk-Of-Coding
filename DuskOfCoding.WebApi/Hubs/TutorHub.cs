using Microsoft.AspNetCore.SignalR;
using DuskOfCoding.Infrastructure.Messaging;

namespace DuskOfCoding.WebApi.Hubs;

/// <summary>
/// SignalR Hub for real-time tutor feedback delivery and Lightbulb 💡 interactive hints.
/// The WebUI connects here to receive live tutor responses for submissions and hints.
/// </summary>
public sealed class TutorHub : Hub
{
    private readonly RabbitMQService _rabbitMqService;
    private readonly ILogger<TutorHub> _logger;

    public TutorHub(RabbitMQService rabbitMqService, ILogger<TutorHub> logger)
    {
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    /// <summary>
    /// Client joins a group keyed by submission ID so it only receives
    /// tutor responses for its own submission.
    /// </summary>
    public async Task JoinSubmission(string submissionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, submissionId);
        _logger.LogInformation(
            "Client {ConnectionId} joined submission group {SubmissionId}",
            Context.ConnectionId, submissionId);
    }

    /// <summary>
    /// Client leaves the submission group when no longer interested.
    /// </summary>
    public async Task LeaveSubmission(string submissionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, submissionId);
        _logger.LogInformation(
            "Client {ConnectionId} left submission group {SubmissionId}",
            Context.ConnectionId, submissionId);
    }

    /// <summary>
    /// Triggered explicitly when the student interacts with the Lightbulb 💡 button for a Socratic hint.
    /// </summary>
    public async Task RequestLightbulbHint(string taskId, string submissionId, string draftCode, string? question, string? language)
    {
        if (!Guid.TryParse(submissionId, out var subId))
        {
            subId = Guid.NewGuid();
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, subId.ToString());

        var message = new InteractiveHintRequestMessage
        {
            TaskId = Guid.TryParse(taskId, out var tid) ? tid : Guid.Empty,
            SubmissionId = subId,
            DraftSourceCode = draftCode,
            UserQuestion = question,
            PreferredLanguage = language ?? "en"
        };

        var correlationId = Guid.NewGuid();
        await _rabbitMqService.PublishAsync(
            RabbitMQTopology.SubmissionExchange,
            RabbitMQTopology.InteractiveTutorRoutingKey,
            message,
            correlationId);

        _logger.LogInformation(
            "Client {ConnectionId} triggered Lightbulb hint for task {TaskId} (CorrelationId: {CorrelationId})",
            Context.ConnectionId, taskId, correlationId);
    }
}
