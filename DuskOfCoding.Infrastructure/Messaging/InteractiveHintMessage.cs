namespace DuskOfCoding.Infrastructure.Messaging;

/// <summary>
/// DTO representing an interactive real-time hint or question sent by a student.
/// </summary>
public sealed class InteractiveHintRequestMessage
{
    public Guid TaskId { get; set; }
    public Guid SubmissionId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string DraftSourceCode { get; set; } = string.Empty;
    public string? UserQuestion { get; set; }
    public string PreferredLanguage { get; set; } = "en";
}
