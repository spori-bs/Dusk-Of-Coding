using PracticePlatform.Domain.Entities;
using PracticePlatform.Domain.Models;

namespace PracticePlatform.Domain.Interfaces;

public interface ICodeExecutionEngine
{
    Task<ExecutionResult> ExecuteAsync(
        TaskDefinition task,
        Submission submission,
        CancellationToken ct = default);
}
