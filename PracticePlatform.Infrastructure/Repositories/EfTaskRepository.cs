using Microsoft.EntityFrameworkCore;
using PracticePlatform.Domain.Entities;
using PracticePlatform.Domain.Interfaces;
using PracticePlatform.Infrastructure.Persistence;

namespace PracticePlatform.Infrastructure.Repositories;

public class EfTaskRepository : ITaskRepository
{
    private readonly AppDbContext _db;

    public EfTaskRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TaskDefinition>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Tasks.AsNoTracking().ToListAsync(ct);
    }

    public async Task<TaskDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
    }
}
