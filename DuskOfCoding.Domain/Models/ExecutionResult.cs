namespace DuskOfCoding.Domain.Models;

public record ExecutionResult
{
    public bool CompilationSucceeded { get; init; }
    public IReadOnlyList<string> CompilationErrors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<TestResult> Tests { get; init; } = Array.Empty<TestResult>();
    public long TotalDurationMs { get; init; }
    public IReadOnlyList<string> RuntimeErrors { get; init; } = Array.Empty<string>();
}

public record TestResult(string Name, bool Passed, string? Message, long DurationMs);
