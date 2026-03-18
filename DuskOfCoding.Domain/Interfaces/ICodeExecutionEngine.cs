using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Models;

namespace DuskOfCoding.Domain.Interfaces;

public interface ICodeExecutionEngine
{
    Task<ExecutionResult> ExecuteAsync(
        TaskDefinition task,
        Submission submission,
        CancellationToken ct = default);
}
