using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Interfaces;
using DuskOfCoding.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DuskOfCoding.Infrastructure.Repositories;

public class EfFeedbackRepository : IFeedbackRepository
{
    private readonly AppDbContext _db;

    public EfFeedbackRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserFeedback?> GetFeedbackAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        return await _db.UserFeedbacks.FirstOrDefaultAsync(f => f.TaskId == taskId && f.UserId == userId, ct);
    }

    public async Task AddAsync(UserFeedback feedback, CancellationToken ct = default)
    {
        _db.UserFeedbacks.Add(feedback);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UserFeedback feedback, CancellationToken ct = default)
    {
        var entry = _db.Entry(feedback);
        if (entry.State == EntityState.Detached)
        {
            _db.UserFeedbacks.Update(feedback);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<UserFeedback>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default)
    {
        return await _db.UserFeedbacks.Where(f => f.TaskId == taskId).ToListAsync(ct);
    }

    public async Task<List<UserFeedback>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.UserFeedbacks.ToListAsync(ct);
    }
}
