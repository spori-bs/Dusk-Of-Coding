using PracticePlatform.Domain.Entities;

namespace PracticePlatform.Application.Services;

public interface ITaskService
{
    Task<IReadOnlyList<TaskDefinition>> GetAllTasksAsync(CancellationToken ct = default);
    Task<TaskDefinition?> GetTaskByIdAsync(Guid id, CancellationToken ct = default);
}
