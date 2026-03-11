using PracticePlatform.Domain.Entities;

namespace PracticePlatform.Domain.Interfaces;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskDefinition>> GetAllAsync(CancellationToken ct = default);
    Task<TaskDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(TaskDefinition task, CancellationToken ct = default);
    Task UpdateAsync(TaskDefinition task, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
