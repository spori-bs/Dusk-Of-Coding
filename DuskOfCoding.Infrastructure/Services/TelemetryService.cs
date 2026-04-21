using Microsoft.EntityFrameworkCore;
using DuskOfCoding.Application.DTOs;
using DuskOfCoding.Application.Services;
using DuskOfCoding.Infrastructure.Persistence;

namespace DuskOfCoding.Infrastructure.Services;

public class TelemetryService : ITelemetryService
{
    private readonly AppDbContext _db;

    public TelemetryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<LlmTelemetryDto>> GetRecentTelemetryAsync(int limit = 50, CancellationToken ct = default)
    {
        return await _db.LlmTelemetryLogs
            .Include(t => t.Task)
            .OrderByDescending(t => t.Timestamp)
            .Take(limit)
            .Select(t => new LlmTelemetryDto
            {
                Id = t.Id,
                Timestamp = t.Timestamp,
                UserId = t.UserId,
                TaskId = t.TaskId,
                TaskTitle = t.Task != null ? t.Task.Title : null,
                ModelName = t.ModelName,
                TokenCount = t.TokenCount,
                IsSuccess = t.IsSuccess
            })
            .ToListAsync(ct);
    }

    public async Task<List<DailyTokenUsageDto>> GetTokenUsageLast7DaysAsync(CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-7);

        var raw = await _db.LlmTelemetryLogs
            .Where(t => t.Timestamp >= cutoff)
            .Select(t => new { t.Timestamp, t.TokenCount })
            .ToListAsync(ct);

        return raw
            .GroupBy(t => DateOnly.FromDateTime(t.Timestamp.UtcDateTime))
            .Select(g => new DailyTokenUsageDto
            {
                Date = g.Key,
                TotalTokens = g.Sum(t => t.TokenCount)
            })
            .OrderBy(d => d.Date)
            .ToList();
    }
}
