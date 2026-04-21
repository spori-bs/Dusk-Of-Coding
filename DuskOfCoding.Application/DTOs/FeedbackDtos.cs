namespace DuskOfCoding.Application.DTOs;

public class CreateFeedbackDto
{
    public Guid TaskId { get; set; }
    
    // Rating 1-5
    public int Rating { get; set; }
    
    // Optional comment
    public string? Comment { get; set; }
    
    // Type of feedback: "rating", "bug_report", "suggestion"
    public string FeedbackType { get; set; } = "rating";
}

public class FeedbackSummaryDto
{
    public Guid TaskId { get; set; }
    public double AverageRating { get; set; }
    public int TotalFeedbackCount { get; set; }
    
    // Rating distribution: e.g., { 5: 10, 4: 2, 3: 0, 2: 1, 1: 0 }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
    
    // Recent comments
    public List<FeedbackCommentDto> RecentComments { get; set; } = new();
}

public class FeedbackCommentDto
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string FeedbackType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TaskFeedbackOverviewDto
{
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int FeedbackCount { get; set; }
    public DateTime LatestFeedbackDate { get; set; }
}

public class RecentFeedbackItemDto
{
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string FeedbackType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
