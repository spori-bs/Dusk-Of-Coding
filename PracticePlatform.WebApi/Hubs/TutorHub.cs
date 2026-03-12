using Microsoft.AspNetCore.SignalR;

namespace PracticePlatform.WebApi.Hubs;

/// <summary>
/// SignalR Hub for real-time tutor feedback delivery.
/// The WebUI connects here to receive live tutor responses for submissions.
/// </summary>
public sealed class TutorHub : Hub
{
    private readonly ILogger<TutorHub> _logger;

    public TutorHub(ILogger<TutorHub> logger)
    {
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
}
