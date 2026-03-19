namespace DuskOfCoding.Domain.Entities;

public class TaskDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DifficultyLevel { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Reference to the test bundle (e.g., path to xUnit project template) 
    /// required by the Execution API to evaluate this task.
    /// </summary>
    public string TestBundleReference { get; set; } = string.Empty;
}
