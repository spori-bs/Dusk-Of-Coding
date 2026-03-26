using DuskOfCoding.Domain.Entities;

namespace DuskOfCoding.Domain.Interfaces;

public interface IFeedbackRepository
{
    Task<UserFeedback?> GetFeedbackAsync(Guid taskId, Guid userId, CancellationToken ct = default);
    Task AddAsync(UserFeedback feedback, CancellationToken ct = default);
    Task UpdateAsync(UserFeedback feedback, CancellationToken ct = default);
    Task<List<UserFeedback>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default);
    Task<List<UserFeedback>> GetAllAsync(CancellationToken ct = default);
}
