namespace DuskOfCoding.Domain.Entities;

public class Submission
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid TaskId { get; init; }
    public Guid? UserId { get; init; }
    public string SourceCode { get; init; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
