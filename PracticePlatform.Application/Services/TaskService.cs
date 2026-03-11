using PracticePlatform.Application.DTOs;
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

    public async Task<TaskDefinition> CreateTaskAsync(CreateTaskDto dto, CancellationToken ct = default)
    {
        var task = new TaskDefinition
        {
            Title = dto.Title,
            Description = dto.Description,
            DifficultyLevel = dto.DifficultyLevel,
            Tags = dto.Tags ?? new List<string>(),
            TestBundleReference = dto.TestBundleReference
        };

        await _taskRepository.AddAsync(task, ct);
        return task;
    }

    public async Task<TaskDefinition?> UpdateTaskAsync(Guid id, UpdateTaskDto dto, CancellationToken ct = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, ct);
        if (task == null) return null;

        var updatedTask = new TaskDefinition
        {
            Id = id,
            Title = dto.Title,
            Description = dto.Description,
            DifficultyLevel = dto.DifficultyLevel,
            Tags = dto.Tags ?? new List<string>(),
            TestBundleReference = dto.TestBundleReference
        };

        await _taskRepository.UpdateAsync(updatedTask, ct);
        return updatedTask;
    }

    public async Task<bool> DeleteTaskAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, ct);
        if (task == null) return false;

        await _taskRepository.DeleteAsync(id, ct);
        return true;
    }
}
