using System.Net.Http.Json;
using System.Net.Http.Headers;
using DuskOfCoding.Domain.Enums;
using DuskOfCoding.Domain.Common;
using Microsoft.Extensions.Localization;
using DuskOfCoding.WebUi.Resources;

namespace DuskOfCoding.WebUi.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly TokenProvider _tokenProvider;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ApiClient(HttpClient http, TokenProvider tokenProvider, IStringLocalizer<SharedResource> localizer)
    {
        _http = http;
        _tokenProvider = tokenProvider;
        _localizer = localizer;
        
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

    public async Task<Result<List<TaskDto>>> GetTasksAsync()
    {
        try
        {
            var response = await _http.GetAsync("/tasks");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<TaskDto>>() ?? new();
                return Result<List<TaskDto>>.Success(data);
            }
            return Result<List<TaskDto>>.Failure(_localizer["Error_FetchTasks", response.StatusCode]);
        }
        catch (HttpRequestException ex)
        {
            return Result<List<TaskDto>>.Failure(_localizer["Error_Network", ex.Message]);
        }
    }

    public async Task<Result<TaskDto>> GetTaskByIdAsync(Guid id)
    {
        try
        {
            var response = await _http.GetAsync($"/tasks/{id}");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<TaskDto>();
                return Result<TaskDto>.Success(data!);
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Result<TaskDto>.Failure(_localizer["Error_TaskNotFound"]);
            
            return Result<TaskDto>.Failure(_localizer["Error_FetchTask", response.StatusCode]);
        }
        catch (HttpRequestException ex)
        {
            return Result<TaskDto>.Failure(_localizer["Error_Network", ex.Message]);
        }
    }

    public async Task<Result<TaskDto>> CreateTaskAsync(CreateTaskDto dto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/tasks", dto);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<TaskDto>();
                return Result<TaskDto>.Success(data!);
            }
            var err = await response.Content.ReadAsStringAsync();
            return Result<TaskDto>.Failure(_localizer["Error_CreateTask", response.StatusCode, err]);
        }
        catch (HttpRequestException ex)
        {
            return Result<TaskDto>.Failure(_localizer["Error_Network", ex.Message]);
        }
    }

    public async Task<Result<TaskDto>> UpdateTaskAsync(Guid id, UpdateTaskDto dto)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/tasks/{id}", dto);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<TaskDto>();
                return Result<TaskDto>.Success(data!);
            }
            var err = await response.Content.ReadAsStringAsync();
            return Result<TaskDto>.Failure(_localizer["Error_UpdateTask", response.StatusCode, err]);
        }
        catch (HttpRequestException ex)
        {
            return Result<TaskDto>.Failure(_localizer["Error_Network", ex.Message]);
        }
    }

    public async Task<Result> DeleteTaskAsync(Guid id)
    {
        try
        {
            var response = await _http.DeleteAsync($"/tasks/{id}");
            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }
            return Result.Failure(_localizer["Error_DeleteTask", response.StatusCode]);
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure(_localizer["Error_Network", ex.Message]);
        }
    }

    // ---- Submissions ----

    public async Task<Result<SubmissionResultDto>> SubmitCodeAsync(Guid taskId, string sourceCode, string language = nameof(ProgrammingLanguage.CSharp), CancellationToken ct = default)
    {
        try
        {
            var payload = new SubmitCodeDto 
            { 
                TaskId = taskId, 
                SourceCode = sourceCode,
                Language = language
            };
            var response = await _http.PostAsJsonAsync("/submissions", payload, ct);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<SubmissionResultDto>(cancellationToken: ct);
                return Result<SubmissionResultDto>.Success(data!);
            }
            var err = await response.Content.ReadAsStringAsync(ct);
            return Result<SubmissionResultDto>.Failure(_localizer["Error_SubmitCode", response.StatusCode, err]);
        }
        catch (HttpRequestException ex)
        {
            return Result<SubmissionResultDto>.Failure(_localizer["Error_Network", ex.Message]);
        }
    }

    public async Task<Result<SubmissionResultDto>> GetSubmissionByIdAsync(Guid id)
    {
        try
        {
            var response = await _http.GetAsync($"/submissions/{id}");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<SubmissionResultDto>();
                return Result<SubmissionResultDto>.Success(data!);
            }
            return Result<SubmissionResultDto>.Failure(_localizer["Error_FetchSubmission", response.StatusCode]);
        }
        catch (HttpRequestException ex)
        {
            return Result<SubmissionResultDto>.Failure(_localizer["Error_Network", ex.Message]);
        }
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
