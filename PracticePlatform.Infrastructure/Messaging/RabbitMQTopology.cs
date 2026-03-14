namespace PracticePlatform.Infrastructure.Messaging;

/// <summary>
/// Constants defining the RabbitMQ exchange/queue topology for the AI-Tutor Platform.
/// </summary>
public static class RabbitMQTopology
{
    // ── Exchanges ──────────────────────────────────────────────
    /// <summary>Direct exchange where code submissions are published.</summary>
    public const string SubmissionExchange = "practice.submission";

    /// <summary>Direct exchange where tutor responses are published back.</summary>
    public const string ResponseExchange = "practice.response";

    // ── Queues ─────────────────────────────────────────────────
    /// <summary>Queue consumed by the TutorWorker to process submissions.</summary>
    public const string TutorInteractionsQueue = "tutor.interactions";

    /// <summary>Queue consumed by the SignalR bridge to push responses to the UI.</summary>
    public const string TutorResponsesQueue = "tutor.responses";

    // ── Routing Keys ───────────────────────────────────────────
    public const string SubmissionRoutingKey = "submission.new";
    public const string ResponseRoutingKey = "response.tutor";
}
