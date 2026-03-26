namespace DuskOfCoding.WebUi.DTOs;

public class TaskFeedbackOverviewDto
{
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int FeedbackCount { get; set; }
    public DateTime LatestFeedbackDate { get; set; }
}
