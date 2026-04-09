using System.Collections.Concurrent;
using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Interfaces;

namespace DuskOfCoding.Infrastructure.Repositories;

public class InMemoryTaskRepository : ITaskRepository
{
    private readonly ConcurrentDictionary<Guid, TaskDefinition> _tasks = new();

    public InMemoryTaskRepository()
    {
        // Seed some initial data
        var defaultTask = new TaskDefinition
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000000"),
            Title = "HelloWorld",
            Description = "Write a method that returns 'Hello World!'",
            DifficultyLevel = "Easy",
            Tags = new List<string> { "fundamentals" },
            Tests = new List<TaskTest>
            {
                new TaskTest
                {
                    Id = Guid.NewGuid(),
                    Name = "BasicTests.cs",
                    Code = "using Xunit; public class T { [Fact] public void C() { Assert.True(true); } }"
                }
            }
        };
        _tasks.TryAdd(defaultTask.Id, defaultTask);
    }

    public Task<IReadOnlyList<TaskDefinition>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<TaskDefinition>>(_tasks.Values.ToList());
    }

    public Task<TaskDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _tasks.TryGetValue(id, out var task);
        return Task.FromResult(task);
    }

    public Task AddAsync(TaskDefinition task, CancellationToken ct = default)
    {
        _tasks.TryAdd(task.Id, task);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TaskDefinition task, CancellationToken ct = default)
    {
        if (_tasks.ContainsKey(task.Id))
        {
            _tasks[task.Id] = task;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _tasks.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
