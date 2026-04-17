namespace DuskOfCoding.Domain.Entities;

public class TaskTest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid TaskDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonIgnore]
    public TaskDefinition? TaskDefinition { get; set; }
}
