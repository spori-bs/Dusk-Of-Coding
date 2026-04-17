using DuskOfCoding.Application.DTOs;
using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Interfaces;

namespace DuskOfCoding.Application.Services;

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
            Tests = (dto.Tests ?? new List<TaskTestDto>()).Select(t => new TaskTest
            {
                Name = t.Name,
                Code = t.Code
            }).ToList(),
            ExpectedClassName = dto.ExpectedClassName ?? "Solution"
        };

        await _taskRepository.AddAsync(task, ct);
        return task;
    }

    public async Task<TaskDefinition?> UpdateTaskAsync(Guid id, UpdateTaskDto dto, CancellationToken ct = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, ct);
        if (task == null) return null;

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.DifficultyLevel = dto.DifficultyLevel;
        task.Tags = dto.Tags ?? new List<string>();
        task.ExpectedClassName = dto.ExpectedClassName ?? "Solution";
        
        // Save the main task definition first (without touching task.Tests)
        await _taskRepository.UpdateAsync(task, ct);

        // Phase 22.3: Disconnected replacement for tests to avoid DbUpdateConcurrencyException
        var newTests = dto.Tests?.Select(t => new TaskTest
        {
            Name = t.Name,
            Code = t.Code
        }).ToList() ?? new List<TaskTest>();

        await _taskRepository.ReplaceTestsAsync(id, newTests, ct);

        // Assign the new tests to the domain object before returning
        task.Tests = newTests;

        return task;
    }

    public async Task<bool> DeleteTaskAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, ct);
        if (task == null) return false;

        await _taskRepository.DeleteAsync(id, ct);
        return true;
    }
}
