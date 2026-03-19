using System.Text.Json;

namespace DuskOfCoding.Domain.Entities;

public class FeedbackRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SubmissionId { get; init; }
    
    public bool IsSuccess { get; set; }
    public string Summary { get; set; } = string.Empty;
    
    // Stored as JSON strings in the database
    public string CompilationMessagesJson { get; set; } = "[]";
    public string TestMessagesJson { get; set; } = "[]";
    
    public string? AiReviewRemarks { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Helper properties (Not mapped to DB natively unles configured with ValueConverter)
    // We'll store them explicitly as strings and use these wrappers.
    public List<string> GetCompilationMessages() => 
        JsonSerializer.Deserialize<List<string>>(CompilationMessagesJson) ?? new List<string>();
        
    public void SetCompilationMessages(List<string> messages) =>
        CompilationMessagesJson = JsonSerializer.Serialize(messages);

    public List<string> GetTestMessages() => 
        JsonSerializer.Deserialize<List<string>>(TestMessagesJson) ?? new List<string>();
        
    public void SetTestMessages(List<string> messages) =>
        TestMessagesJson = JsonSerializer.Serialize(messages);
}
