using DuskOfCoding.Application.DTOs;
using DuskOfCoding.Domain.Entities;
using DuskOfCoding.Domain.Interfaces;

namespace DuskOfCoding.Application.Services;

public class FeedbackService : IFeedbackService
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly ITaskRepository _taskRepository;

    public FeedbackService(IFeedbackRepository feedbackRepository, ITaskRepository taskRepository)
    {
        _feedbackRepository = feedbackRepository;
        _taskRepository = taskRepository;
    }

    public async Task<UserFeedback> SubmitFeedbackAsync(Guid userId, CreateFeedbackDto dto, CancellationToken ct = default)
    {
        var existing = await _feedbackRepository.GetFeedbackAsync(dto.TaskId, userId, ct);

        if (existing != null)
        {
            existing.Rating = dto.Rating;
            existing.Comment = dto.Comment;
            existing.FeedbackType = dto.FeedbackType;
            existing.CreatedAt = DateTime.UtcNow;
            
            await _feedbackRepository.UpdateAsync(existing, ct);
            return existing;
        }

        var newFeedback = new UserFeedback
        {
            TaskId = dto.TaskId,
            UserId = userId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            FeedbackType = dto.FeedbackType,
            CreatedAt = DateTime.UtcNow
        };

        await _feedbackRepository.AddAsync(newFeedback, ct);
        return newFeedback;
    }

    public async Task<FeedbackSummaryDto> GetTaskFeedbackSummaryAsync(Guid taskId, CancellationToken ct = default)
    {
        var feedback = await _feedbackRepository.GetByTaskIdAsync(taskId, ct);

        var count = feedback.Count;
        var avg = count > 0 && feedback.Any(f => f.Rating > 0) 
            ? feedback.Where(f => f.Rating > 0).Average(f => f.Rating) 
            : 0;

        var dist = feedback.Where(f => f.Rating > 0).GroupBy(f => f.Rating)
            .ToDictionary(g => g.Key, g => g.Count());

        for (int i = 1; i <= 5; i++)
        {
            if (!dist.ContainsKey(i)) dist[i] = 0;
        }

        var recent = feedback.OrderByDescending(f => f.CreatedAt)
            .Take(10)
            .Select(f => new FeedbackCommentDto
            {
                Rating = f.Rating,
                Comment = f.Comment,
                FeedbackType = f.FeedbackType,
                CreatedAt = f.CreatedAt
            }).ToList();

        return new FeedbackSummaryDto
        {
            TaskId = taskId,
            AverageRating = Math.Round(avg, 1),
            TotalFeedbackCount = count,
            RatingDistribution = dist,
            RecentComments = recent
        };
    }

    public async Task<List<TaskFeedbackOverviewDto>> GetPlatformFeedbackOverviewAsync(CancellationToken ct = default)
    {
        var allFeedback = await _feedbackRepository.GetAllAsync(ct);
        var allTasks = await _taskRepository.GetAllAsync(ct);
        var taskDict = allTasks.ToDictionary(t => t.Id, t => t.Title);

        var overview = allFeedback
            .GroupBy(f => f.TaskId)
            .Select(g => new TaskFeedbackOverviewDto
            {
                TaskId = g.Key,
                TaskTitle = taskDict.TryGetValue(g.Key, out var title) ? title : "Unknown Task",
                FeedbackCount = g.Count(),
                AverageRating = Math.Round(g.Any(x => x.Rating > 0) ? g.Where(x => x.Rating > 0).Average(x => (double)x.Rating) : 0, 1),
                LatestFeedbackDate = g.Max(x => x.CreatedAt)
            })
            .OrderBy(x => x.AverageRating)
            .ToList();

        return overview;
    }

    public async Task<List<RecentFeedbackItemDto>> GetRecentFeedbackAsync(int limit = 20, CancellationToken ct = default)
    {
        var allFeedback = await _feedbackRepository.GetAllAsync(ct);
        var allTasks = await _taskRepository.GetAllAsync(ct);
        var taskDict = allTasks.ToDictionary(t => t.Id, t => t.Title);

        return allFeedback
            .Where(f => !string.IsNullOrWhiteSpace(f.Comment))
            .OrderByDescending(f => f.CreatedAt)
            .Take(limit)
            .Select(f => new RecentFeedbackItemDto
            {
                TaskId = f.TaskId,
                TaskTitle = taskDict.TryGetValue(f.TaskId, out var title) ? title : f.TaskId.ToString(),
                Rating = f.Rating,
                Comment = f.Comment,
                FeedbackType = f.FeedbackType,
                CreatedAt = f.CreatedAt
            })
            .ToList();
    }
}
