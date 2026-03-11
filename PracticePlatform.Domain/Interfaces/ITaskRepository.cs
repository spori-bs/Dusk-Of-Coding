using PracticePlatform.Domain.Entities;

namespace PracticePlatform.Domain.Interfaces;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskDefinition>> GetAllAsync(CancellationToken ct = default);
    Task<TaskDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
