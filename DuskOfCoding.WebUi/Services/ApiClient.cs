using System.Net.Http.Json;
using System.Net.Http.Headers;
using DuskOfCoding.Domain.Enums;
using DuskOfCoding.Domain.Common;
using Microsoft.Extensions.Localization;
using DuskOfCoding.WebUi.Resources;
using Microsoft.AspNetCore.Components;

namespace DuskOfCoding.WebUi.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly TokenProvider _tokenProvider;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly NavigationManager _nav;

    public ApiClient(HttpClient http, TokenProvider tokenProvider, IStringLocalizer<SharedResource> localizer, NavigationManager nav)
    {
        _http = http;
        _tokenProvider = tokenProvider;
        _localizer = localizer;
        _nav = nav;
    }

    private void EnsureAuthHeader()
    {
        if (!string.IsNullOrEmpty(_tokenProvider.AccessToken) && _http.DefaultRequestHeaders.Authorization == null)
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokenProvider.AccessToken);
        }
    }

    private bool HandleAuthErrors(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var returnUrl = Uri.EscapeDataString(new Uri(_nav.Uri).PathAndQuery);
            _nav.NavigateTo($"/login?returnUrl={returnUrl}", forceLoad: true);
            return true;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _nav.NavigateTo("/access-denied", forceLoad: true);
            return true;
        }
        return false;
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
            EnsureAuthHeader();
            var response = await _http.GetAsync("/tasks");
            if (HandleAuthErrors(response)) return Result<List<TaskDto>>.Failure(string.Empty);
            
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
            EnsureAuthHeader();
            var response = await _http.GetAsync($"/tasks/{id}");
            if (HandleAuthErrors(response)) return Result<TaskDto>.Failure(string.Empty);

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
            EnsureAuthHeader();
            var response = await _http.PostAsJsonAsync("/tasks", dto);
            if (HandleAuthErrors(response)) return Result<TaskDto>.Failure(string.Empty);

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
            EnsureAuthHeader();
            var response = await _http.PutAsJsonAsync($"/tasks/{id}", dto);
            if (HandleAuthErrors(response)) return Result<TaskDto>.Failure(string.Empty);

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
            EnsureAuthHeader();
            var response = await _http.DeleteAsync($"/tasks/{id}");
            if (HandleAuthErrors(response)) return Result.Failure(string.Empty);

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
            EnsureAuthHeader();
            var payload = new SubmitCodeDto 
            { 
                TaskId = taskId, 
                SourceCode = sourceCode,
                Language = language
            };
            var response = await _http.PostAsJsonAsync("/submissions", payload, ct);
            if (HandleAuthErrors(response)) return Result<SubmissionResultDto>.Failure(string.Empty);
            
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
            EnsureAuthHeader();
            var response = await _http.GetAsync($"/submissions/{id}");
            if (HandleAuthErrors(response)) return Result<SubmissionResultDto>.Failure(string.Empty);

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

    // ---- Admin ----
    public class AdminStatsDto
    {
        public int TotalStudents { get; set; }
        public int TotalTasks { get; set; }
        public int TotalSubmissions { get; set; }
        public double SuccessRate { get; set; }
    }

    public async Task<Result<AdminStatsDto>> GetAdminStatsAsync()
    {
        try
        {
            EnsureAuthHeader();
            var response = await _http.GetAsync("/admin/stats");
            if (HandleAuthErrors(response)) return Result<AdminStatsDto>.Failure(string.Empty);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<AdminStatsDto>();
                return Result<AdminStatsDto>.Success(data!);
            }
            return Result<AdminStatsDto>.Failure($"Failed to fetch analytics: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return Result<AdminStatsDto>.Failure(_localizer["Error_Network", ex.Message]);
        }
    }

    // ---- Feedback ----

    public async Task<Result<object>> SubmitFeedbackAsync(CreateFeedbackDto dto, CancellationToken ct = default)
    {
        try
        {
            EnsureAuthHeader();
            var response = await _http.PostAsJsonAsync("/feedback", dto, ct);
            if (HandleAuthErrors(response)) return Result<object>.Failure(string.Empty);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<object>(cancellationToken: ct);
                return Result<object>.Success(data!);
            }
            var err = await response.Content.ReadAsStringAsync(ct);
            return Result<object>.Failure($"Failed to submit feedback: {response.StatusCode} - {err}");
        }
        catch (HttpRequestException ex)
        {
            return Result<object>.Failure(_localizer["Error_Network", ex.Message]);
        }
    }

    public async Task<Result<FeedbackSummaryDto>> GetFeedbackSummaryAsync(Guid taskId, CancellationToken ct = default)
    {
        try
        {
            EnsureAuthHeader();
            var response = await _http.GetAsync($"/feedback/task/{taskId}/summary", ct);
            if (HandleAuthErrors(response)) return Result<FeedbackSummaryDto>.Failure(string.Empty);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<FeedbackSummaryDto>(cancellationToken: ct);
                return Result<FeedbackSummaryDto>.Success(data!);
            }
            return Result<FeedbackSummaryDto>.Failure($"Failed to load feedback summary: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return Result<FeedbackSummaryDto>.Failure(_localizer["Error_Network", ex.Message]);
        }
    }

    public async Task<Result<List<TaskFeedbackOverviewDto>>> GetAdminFeedbackOverviewAsync(CancellationToken ct = default)
    {
        try
        {
            EnsureAuthHeader();
            var response = await _http.GetAsync("/feedback/overview", ct);
            if (HandleAuthErrors(response)) return Result<List<TaskFeedbackOverviewDto>>.Failure(string.Empty);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<TaskFeedbackOverviewDto>>(cancellationToken: ct) ?? new();
                return Result<List<TaskFeedbackOverviewDto>>.Success(data);
            }
            return Result<List<TaskFeedbackOverviewDto>>.Failure($"Failed to load overview: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return Result<List<TaskFeedbackOverviewDto>>.Failure(_localizer["Error_Network", ex.Message]);
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

public class CreateFeedbackDto
{
    public Guid TaskId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string FeedbackType { get; set; } = "rating";
}

public class FeedbackSummaryDto
{
    public Guid TaskId { get; set; }
    public double AverageRating { get; set; }
    public int TotalFeedbackCount { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
    public List<FeedbackCommentDto> RecentComments { get; set; } = new();
}

public class FeedbackCommentDto
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string FeedbackType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TaskFeedbackOverviewDto
{
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int FeedbackCount { get; set; }
    public DateTime LatestFeedbackDate { get; set; }
}
