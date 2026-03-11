using System.Net.Http.Json;
using PracticePlatform.Domain.Enums;

namespace PracticePlatform.WebUi.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public class SubmitCodeDto
    {
        public Guid TaskId { get; set; }
        public string SourceCode { get; set; } = string.Empty;
        public string Language { get; set; } = "C#"; 
    }

    // ---- Tasks ----

    public async Task<List<TaskDto>> GetTasksAsync()
    {
        return await _http.GetFromJsonAsync<List<TaskDto>>("/tasks") ?? new();
    }

    public async Task<TaskDto?> GetTaskByIdAsync(Guid id)
    {
        return await _http.GetFromJsonAsync<TaskDto>($"/tasks/{id}");
    }

    public async Task<TaskDto?> CreateTaskAsync(CreateTaskDto dto)
    {
        var response = await _http.PostAsJsonAsync("/tasks", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskDto>();
    }

    public async Task<TaskDto?> UpdateTaskAsync(Guid id, UpdateTaskDto dto)
    {
        var response = await _http.PutAsJsonAsync($"/tasks/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskDto>();
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"/tasks/{id}");
        response.EnsureSuccessStatusCode();
    }

    // ---- Submissions ----

    public async Task<SubmissionResultDto?> SubmitCodeAsync(Guid taskId, string sourceCode, string language = nameof(ProgrammingLanguage.CSharp), CancellationToken ct = default)
    {
        var payload = new SubmitCodeDto 
        { 
            TaskId = taskId, 
            SourceCode = sourceCode,
            Language = language
        };
        var response = await _http.PostAsJsonAsync("/submissions", payload, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SubmissionResultDto>();
    }

    public async Task<SubmissionResultDto?> GetSubmissionByIdAsync(Guid id)
    {
        return await _http.GetFromJsonAsync<SubmissionResultDto>($"/submissions/{id}");
    }
}

// ---- DTOs (mirrors WebApi responses) ----

public class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string DifficultyLevel { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public string TestBundleReference { get; set; } = "";
}

public class CreateTaskDto
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string DifficultyLevel { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public string TestBundleReference { get; set; } = "";
}

public class UpdateTaskDto
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string DifficultyLevel { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public string TestBundleReference { get; set; } = "";
}

public class SubmissionResultDto
{
    public SubmissionDto? Submission { get; set; }
    public FeedbackDto? Feedback { get; set; }
}

public class SubmissionDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string SourceCode { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class FeedbackDto
{
    public bool IsSuccess { get; set; }
    public string Summary { get; set; } = "";
    public List<string>? CompilationMessages { get; set; }
    public List<string>? TestMessages { get; set; }
    public string? AiReviewRemarks { get; set; }
}
