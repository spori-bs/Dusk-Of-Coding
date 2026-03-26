namespace DuskOfCoding.WebUi.DTOs;

public class CreateTaskDto
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string DifficultyLevel { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public string TestBundleReference { get; set; } = "";
}
