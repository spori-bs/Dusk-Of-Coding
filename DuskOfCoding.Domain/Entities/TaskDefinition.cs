namespace DuskOfCoding.Domain.Entities;

public class TaskDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DifficultyLevel { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = new();
    
    /// <summary>
    /// Reference to the test bundle (e.g., path to xUnit project template) 
    /// required by the Execution API to evaluate this task.
    /// </summary>
    public string TestBundleReference { get; init; } = string.Empty;
}
