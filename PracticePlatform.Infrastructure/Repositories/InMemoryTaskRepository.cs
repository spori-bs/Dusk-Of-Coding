using System.Collections.Concurrent;
using PracticePlatform.Domain.Entities;
using PracticePlatform.Domain.Interfaces;

namespace PracticePlatform.Infrastructure.Repositories;

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
            TestBundleReference = "tasks/helloworld/tests.csproj"
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
}
