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
