using System.Net.Http.Json;
using System.Net.Http.Headers;
using DuskOfCoding.Domain.Enums;

namespace DuskOfCoding.WebUi.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly TokenProvider _tokenProvider;

    public ApiClient(HttpClient http, TokenProvider tokenProvider)
    {
        _http = http;
        _tokenProvider = tokenProvider;
        
        if (!string.IsNullOrEmpty(_tokenProvider.AccessToken))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokenProvider.AccessToken);
        }
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
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
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
