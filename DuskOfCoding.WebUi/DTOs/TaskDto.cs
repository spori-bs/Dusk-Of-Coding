namespace DuskOfCoding.WebUi.DTOs;

public class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string DifficultyLevel { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public List<TaskTestDto> Tests { get; set; } = new();
    public string ExpectedClassName { get; set; } = "Solution";
}
