using System.Collections.Concurrent;
using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Interfaces;

namespace DuskOfCoding.Infrastructure.Repositories;

public class InMemorySubmissionRepository : ISubmissionRepository
{
    private readonly ConcurrentDictionary<Guid, Submission> _submissions = new();

    public Task<Submission> AddAsync(Submission submission, CancellationToken ct = default)
    {
        _submissions.TryAdd(submission.Id, submission);
        return Task.FromResult(submission);
    }

    public Task<Submission?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _submissions.TryGetValue(id, out var submission);
        return Task.FromResult(submission);
    }

    public Task UpdateAsync(Submission submission, CancellationToken ct = default)
    {
        _submissions.AddOrUpdate(submission.Id, submission, (_, _) => submission);
        return Task.CompletedTask;
    }
}
