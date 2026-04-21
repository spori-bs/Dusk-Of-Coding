namespace DuskOfCoding.WebUi.DTOs;

public class RecentFeedbackItemDto
{
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string FeedbackType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
