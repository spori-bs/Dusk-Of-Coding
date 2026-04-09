using System.Text.Json.Serialization;

namespace DuskOfCoding.Infrastructure.Messaging;

/// <summary>
/// Message published to SubmissionExchange when a student submits code.
/// </summary>
public sealed record SubmissionMessage
{
    public required Guid CorrelationId { get; init; }
    public required Guid TaskId { get; init; }
    public required Guid SubmissionId { get; init; }
    public required string SourceCode { get; init; }
    public required string Language { get; init; }
    public string? PreferredLanguage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Message published to ResponseExchange when the tutor produces a response.
/// </summary>
public sealed record TutorResponseMessage
{
    public required Guid CorrelationId { get; init; }
    public required Guid SubmissionId { get; init; }
    public required string ResponseType { get; init; } // "hint", "feedback", "error", "safety"
    public required string Content { get; init; }
    public string? RawDiagnostics { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Command published to TestGenerationExchange when a tutor requests AI test suite generation.
/// Consumed by TestGenerationWorkerService in TutorWorker.
/// </summary>
public sealed record GenerateTestSuiteCommand
{
    public required Guid TaskId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    /// <summary>Keycloak sub-claim (user ID) for targeted SignalR notification.</summary>
    public required string UserId { get; init; }
}

/// <summary>
/// Result published by TutorWorker after test generation completes (success or failure).
/// Consumed by TestGenerationBridge in WebApi to push to the originating user via SignalR.
/// </summary>
public sealed record TestGenerationResultMessage
{
    public required Guid TaskId { get; init; }
    public required string UserId { get; init; }
    public required bool IsSuccess { get; init; }
    /// <summary>Localization key passed to the UI for display (e.g. "Test_Suite_Success_Key").</summary>
    public required string MessageKey { get; init; }
}
