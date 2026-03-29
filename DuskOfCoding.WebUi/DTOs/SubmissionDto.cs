using DuskOfCoding.Domain.Enums;

namespace DuskOfCoding.WebUi.DTOs;

public class SubmissionDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string SourceCode { get; set; } = "";
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
