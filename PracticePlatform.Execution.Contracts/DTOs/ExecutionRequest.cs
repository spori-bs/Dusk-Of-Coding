namespace PracticePlatform.Execution.Contracts.DTOs;

public record ExecutionRequest
{
    public Guid TaskId { get; init; }
    public Guid SubmissionId { get; init; }
    public string SourceCode { get; init; } = string.Empty;
    public string Language { get; init; } = "csharp";
    public TestBundleDto? TestBundle { get; init; }
    public LimitsDto? Limits { get; init; }
}

public record TestBundleDto
{
    public string Framework { get; init; } = "xunit";
    public string ProjectTemplatePath { get; init; } = string.Empty;
}

public record LimitsDto
{
    public int CpuSeconds { get; init; } = 5;
    public int MemoryMb { get; init; } = 256;
    public int WallClockSeconds { get; init; } = 15;
}
