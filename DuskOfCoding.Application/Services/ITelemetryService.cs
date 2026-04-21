using DuskOfCoding.Application.DTOs;

namespace DuskOfCoding.Application.Services;

public interface ITelemetryService
{
    Task<List<LlmTelemetryDto>> GetRecentTelemetryAsync(int limit = 50, CancellationToken ct = default);
    Task<List<DailyTokenUsageDto>> GetTokenUsageLast7DaysAsync(CancellationToken ct = default);
}
