namespace DuskOfCoding.WebUi.DTOs;

public class CreateFeedbackDto
{
    public Guid TaskId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string FeedbackType { get; set; } = "rating";
}
