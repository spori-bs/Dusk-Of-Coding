namespace DuskOfCoding.Domain.Entities;

public class TaskDefinition
{
    public Guid Id { get; init; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DifficultyLevel { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string ExpectedClassName { get; set; } = "Solution";
    
    /// <summary>
    /// Collection of xUnit test files evaluating this task.
    /// </summary>
    public ICollection<TaskTest> Tests { get; set; } = new List<TaskTest>();
}
