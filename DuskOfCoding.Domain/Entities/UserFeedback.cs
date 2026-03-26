namespace DuskOfCoding.Domain.Entities;

public class UserFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    
    // Rating 1-5 stars; can be 0 if only a comment/bug report is provided without a star rating.
    public int Rating { get; set; }
    
    // Optional free-form text
    public string? Comment { get; set; }
    
    // e.g. "rating", "bug_report", "suggestion"
    public string FeedbackType { get; set; } = "rating";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
