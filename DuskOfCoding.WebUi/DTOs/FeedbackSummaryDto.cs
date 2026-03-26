namespace DuskOfCoding.WebUi.DTOs;

public class FeedbackSummaryDto
{
    public Guid TaskId { get; set; }
    public double AverageRating { get; set; }
    public int TotalFeedbackCount { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
    public List<FeedbackCommentDto> RecentComments { get; set; } = new();
}
