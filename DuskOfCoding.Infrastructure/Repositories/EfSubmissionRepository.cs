using Microsoft.EntityFrameworkCore;
using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Interfaces;
using DuskOfCoding.Infrastructure.Persistence;

namespace DuskOfCoding.Infrastructure.Repositories;

public class EfSubmissionRepository : ISubmissionRepository
{
    private readonly AppDbContext _db;

    public EfSubmissionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Submission> AddAsync(Submission submission, CancellationToken ct = default)
    {
        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync(ct);
        return submission;
    }

    public async Task<Submission?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Submissions.Include(s => s.Feedback).FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task UpdateAsync(Submission submission, CancellationToken ct = default)
    {
        var entry = _db.Entry(submission);
        if (entry.State == EntityState.Detached)
        {
            _db.Submissions.Update(submission);
        }
        await _db.SaveChangesAsync(ct);
    }
}
