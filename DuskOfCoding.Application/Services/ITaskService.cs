using DuskOfCoding.Application.DTOs;
using DuskOfCoding.Domain.Entities;

namespace DuskOfCoding.Application.Services;

public interface ITaskService
{
    Task<IReadOnlyList<TaskDefinition>> GetAllTasksAsync(CancellationToken ct = default);
    Task<TaskDefinition?> GetTaskByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaskDefinition> CreateTaskAsync(CreateTaskDto dto, CancellationToken ct = default);
    Task<TaskDefinition?> UpdateTaskAsync(Guid id, UpdateTaskDto dto, CancellationToken ct = default);
    Task<bool> DeleteTaskAsync(Guid id, CancellationToken ct = default);
}
