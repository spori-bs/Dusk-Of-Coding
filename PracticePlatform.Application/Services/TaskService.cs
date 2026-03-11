using PracticePlatform.Domain.Entities;
using PracticePlatform.Domain.Interfaces;

namespace PracticePlatform.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;

    public TaskService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public Task<IReadOnlyList<TaskDefinition>> GetAllTasksAsync(CancellationToken ct = default)
    {
        return _taskRepository.GetAllAsync(ct);
    }

    public Task<TaskDefinition?> GetTaskByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _taskRepository.GetByIdAsync(id, ct);
    }
}
