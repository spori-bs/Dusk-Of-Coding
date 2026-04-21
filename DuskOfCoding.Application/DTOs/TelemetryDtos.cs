namespace DuskOfCoding.Application.DTOs;

public class LlmTelemetryDto
{
    public Guid Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TaskId { get; set; }
    public string? TaskTitle { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public bool IsSuccess { get; set; }
}

public class DailyTokenUsageDto
{
    public DateOnly Date { get; set; }
    public int TotalTokens { get; set; }
}
