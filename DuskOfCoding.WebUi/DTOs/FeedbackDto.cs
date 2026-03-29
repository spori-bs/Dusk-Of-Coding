namespace DuskOfCoding.WebUi.DTOs;

public class FeedbackDto
{
    public bool IsSuccess { get; set; }
    public string Summary { get; set; } = "";
    public List<string>? CompilationMessages { get; set; }
    public List<string>? TestMessages { get; set; }
    public string? AiReviewRemarks { get; set; }
}
