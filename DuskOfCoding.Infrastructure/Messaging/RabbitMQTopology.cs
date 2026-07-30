namespace DuskOfCoding.Infrastructure.Messaging;

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

    /// <summary>Direct exchange where test generation commands are published.</summary>
    public const string TestGenerationExchange = "practice.testgeneration";

    // ── Queues ─────────────────────────────────────────────────
    /// <summary>Queue consumed by the TutorWorker to process submissions.</summary>
    public const string TutorInteractionsQueue = "tutor.interactions";

    /// <summary>Queue consumed by the SignalR bridge to push responses to the UI.</summary>
    public const string TutorResponsesQueue = "tutor.responses";

    /// <summary>Queue consumed by the TestGenerationWorker to process AI generation commands.</summary>
    public const string TestGenerationQueue = "testgeneration.commands";

    /// <summary>Queue consumed by the SignalR bridge to push test generation results to the UI.</summary>
    public const string TestGenerationResponseQueue = "testgeneration.responses";

    /// <summary>Queue consumed by the TutorWorker for real-time interactive hints and chat questions.</summary>
    public const string InteractiveTutorQueue = "tutor.interactive.requests";

    // ── Routing Keys ───────────────────────────────────────────
    public const string SubmissionRoutingKey = "submission.new";
    public const string ResponseRoutingKey = "response.tutor";
    public const string TestGenerationRoutingKey = "testgeneration.new";
    public const string TestGenerationResponseRoutingKey = "response.testgeneration";
    public const string InteractiveTutorRoutingKey = "interactive.request";
}
