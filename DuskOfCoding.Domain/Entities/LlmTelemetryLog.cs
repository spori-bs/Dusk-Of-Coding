using System;

namespace DuskOfCoding.Domain.Entities;

public class LlmTelemetryLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public Guid? UserId { get; init; }
    public Guid? TaskId { get; init; }
    public string ModelName { get; init; } = string.Empty;
    public int TokenCount { get; init; }
    public bool IsSuccess { get; init; }

    // Unindexed "Dump" column for raw request/response data
    public string Payload { get; init; } = string.Empty;

    // Navigation property (optional — TaskId is nullable)
    public TaskDefinition? Task { get; init; }
}
