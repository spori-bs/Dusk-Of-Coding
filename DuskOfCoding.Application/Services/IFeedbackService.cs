using DuskOfCoding.Application.DTOs;
using DuskOfCoding.Domain.Entities;

namespace DuskOfCoding.Application.Services;

public interface IFeedbackService
{
    Task<UserFeedback> SubmitFeedbackAsync(Guid userId, CreateFeedbackDto dto, CancellationToken ct = default);
    Task<FeedbackSummaryDto> GetTaskFeedbackSummaryAsync(Guid taskId, CancellationToken ct = default);
    Task<List<TaskFeedbackOverviewDto>> GetPlatformFeedbackOverviewAsync(CancellationToken ct = default);
}
