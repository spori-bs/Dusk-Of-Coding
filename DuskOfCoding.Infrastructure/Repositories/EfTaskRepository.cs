using Microsoft.EntityFrameworkCore;
using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Interfaces;
using DuskOfCoding.Infrastructure.Persistence;

namespace DuskOfCoding.Infrastructure.Repositories;

public class EfTaskRepository : ITaskRepository
{
    private readonly AppDbContext _db;

    public EfTaskRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TaskDefinition>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Tasks.Include(t => t.Tests).ToListAsync(ct);
    }

    public async Task<TaskDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Tasks.Include(t => t.Tests).FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task AddAsync(TaskDefinition task, CancellationToken ct = default)
    {
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TaskDefinition task, CancellationToken ct = default)
    {
        // Detach associated tests from the tracker before saving task scalars.
        // Tests are managed exclusively via ReplaceTestsAsync — letting them stay
        // tracked here causes phantom state after the subsequent bulk delete.
        foreach (var e in _db.ChangeTracker.Entries<TaskTest>()
            .Where(e => e.Entity.TaskDefinitionId == task.Id).ToList())
        {
            e.State = EntityState.Detached;
        }

        var entry = _db.Entry(task);
        if (entry.State == EntityState.Detached)
        {
            _db.Tasks.Update(task);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _db.Tasks.FindAsync(new object[] { id }, ct);
        if (task != null)
        {
            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task ReplaceTestsAsync(Guid taskId, IEnumerable<TaskTest> tests, CancellationToken ct = default)
    {
        // Detach any stale tracked tests before bulk delete to prevent phantom tracking
        foreach (var e in _db.ChangeTracker.Entries<TaskTest>()
            .Where(e => e.Entity.TaskDefinitionId == taskId).ToList())
        {
            e.State = EntityState.Detached;
        }

        await _db.TaskTests
            .Where(t => t.TaskDefinitionId == taskId)
            .ExecuteDeleteAsync(ct);

        var testList = tests.ToList();
        foreach (var test in testList)
        {
            test.TaskDefinitionId = taskId;
        }

        _db.TaskTests.AddRange(testList);
        await _db.SaveChangesAsync(ct);
    }
}
