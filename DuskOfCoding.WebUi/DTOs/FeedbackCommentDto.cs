namespace DuskOfCoding.WebUi.DTOs;

public class FeedbackCommentDto
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string FeedbackType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
