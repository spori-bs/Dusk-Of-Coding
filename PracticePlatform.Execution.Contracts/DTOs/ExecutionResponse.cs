namespace PracticePlatform.Execution.Contracts.DTOs;

public record ExecutionResponse
{
    public Guid SubmissionId { get; init; }
    public string Status { get; init; } = "Completed";
    public CompilationResultDto? Compilation { get; init; }
    public IReadOnlyList<TestResultDto> Tests { get; init; } = Array.Empty<TestResultDto>();
    public RuntimeMetricsDto? Runtime { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

public record CompilationResultDto
{
    public bool Succeeded { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

public record TestResultDto
{
    public string Name { get; init; } = string.Empty;
    public bool Passed { get; init; }
    public string? Message { get; init; }
    public long DurationMs { get; init; }
}

public record RuntimeMetricsDto
{
    public long TotalDurationMs { get; init; }
}
