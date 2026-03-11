using PracticePlatform.Domain.Entities;

namespace PracticePlatform.Domain.Interfaces;

public interface ISubmissionRepository
{
    Task<Submission> AddAsync(Submission submission, CancellationToken ct = default);
    Task<Submission?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(Submission submission, CancellationToken ct = default);
}
