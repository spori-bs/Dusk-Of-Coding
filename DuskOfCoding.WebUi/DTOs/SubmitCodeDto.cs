namespace DuskOfCoding.WebUi.DTOs;

public class SubmitCodeDto
{
    public Guid TaskId { get; set; }
    public string SourceCode { get; set; } = string.Empty;
    public string Language { get; set; } = "C#"; 
    public string? PreferredLanguage { get; set; }
}
