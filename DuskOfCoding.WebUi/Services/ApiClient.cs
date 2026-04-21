using System.Net.Http.Json;
using System.Net.Http.Headers;
using DuskOfCoding.Domain.Enums;
using DuskOfCoding.Domain.Common;
using Microsoft.Extensions.Localization;
using DuskOfCoding.WebUi.Resources;
using Microsoft.AspNetCore.Components;
using DuskOfCoding.WebUi.DTOs;

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

    private async Task EnsureAuthHeaderAsync()
    {
        var token = await _tokenProvider.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token) && _http.DefaultRequestHeaders.Authorization == null)
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
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


    // ---- Tasks ----

    public async Task<Result<List<TaskDto>>> GetTasksAsync()
    {
        try
        {
            await EnsureAuthHeaderAsync();
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
            await EnsureAuthHeaderAsync();
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
            await EnsureAuthHeaderAsync();
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
            await EnsureAuthHeaderAsync();
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
            await EnsureAuthHeaderAsync();
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

    /// <summary>
    /// Deletes a single persisted test case from a task. (Phase 23)
    /// </summary>
    public async Task<Result> DeleteTestAsync(Guid taskId, Guid testId)
    {
        try
        {
            await EnsureAuthHeaderAsync();
            var response = await _http.DeleteAsync($"/tasks/{taskId}/tests/{testId}");
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

    /// <summary>
    /// Triggers async AI test generation for an existing task.
    /// Returns immediately (202 Accepted). Results are delivered via SignalR.
    /// </summary>
    public async Task<Result> TriggerTestGenerationAsync(Guid taskId)
    {
        try
        {
            await EnsureAuthHeaderAsync();
            var response = await _http.PostAsync($"/tasks/{taskId}/generate-tests", new StringContent(""));
            if (HandleAuthErrors(response)) return Result.Failure(string.Empty);

            // 202 Accepted is the success response for fire-and-forget
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                return Result.Success();
            }
            var err = await response.Content.ReadAsStringAsync();
            return Result.Failure($"Failed to trigger test generation: {response.StatusCode} - {err}");
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure(_localizer["Error_Network", ex.Message]);
        }
    }

    // ---- Submissions ----

    public async Task<Result<SubmissionResultDto>> SubmitCodeAsync(Guid taskId, string sourceCode, string language = nameof(ProgrammingLanguage.CSharp), string? preferredLanguage = null, CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthHeaderAsync();
            var payload = new SubmitCodeDto 
            { 
                TaskId = taskId, 
                SourceCode = sourceCode,
                Language = language,
                PreferredLanguage = preferredLanguage ?? System.Globalization.CultureInfo.CurrentUICulture.Name
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
            await EnsureAuthHeaderAsync();
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

    public async Task<Result<TutorStatsDto>> GetTutorStatsAsync()
    {
        try
        {
            await EnsureAuthHeaderAsync();
            var response = await _http.GetAsync("/tutor/stats");
            if (HandleAuthErrors(response)) return Result<TutorStatsDto>.Failure(string.Empty);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<TutorStatsDto>();
                return Result<TutorStatsDto>.Success(data!);
            }
            return Result<TutorStatsDto>.Failure($"Failed to fetch analytics: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return Result<TutorStatsDto>.Failure(_localizer["Error_Network", ex.Message]);
        }
    }

    // ---- Feedback ----

    public async Task<Result<object>> SubmitFeedbackAsync(CreateFeedbackDto dto, CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthHeaderAsync();
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
            await EnsureAuthHeaderAsync();
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

    public async Task<Result<List<TaskFeedbackOverviewDto>>> GetTutorFeedbackOverviewAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthHeaderAsync();
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

    public async Task<Result<List<RecentFeedbackItemDto>>> GetRecentFeedbackCommentsAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthHeaderAsync();
            var response = await _http.GetAsync("/feedback/recent-comments", ct);
            if (HandleAuthErrors(response)) return Result<List<RecentFeedbackItemDto>>.Failure(string.Empty);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<RecentFeedbackItemDto>>(cancellationToken: ct) ?? new();
                return Result<List<RecentFeedbackItemDto>>.Success(data);
            }
            return Result<List<RecentFeedbackItemDto>>.Failure($"Failed to load recent feedback: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return Result<List<RecentFeedbackItemDto>>.Failure(_localizer["Error_Network", ex.Message]);
        }
    }

    public async Task<Result<LlmProviderStatusDto>> GetLlmProviderAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthHeaderAsync();
            var response = await _http.GetAsync("/system/llm-provider", ct);
            if (HandleAuthErrors(response)) return Result<LlmProviderStatusDto>.Failure(string.Empty);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<LlmProviderStatusDto>(cancellationToken: ct);
                return Result<LlmProviderStatusDto>.Success(data!);
            }
            return Result<LlmProviderStatusDto>.Failure($"Failed to fetch provider status: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return Result<LlmProviderStatusDto>.Failure(_localizer["Error_Network", ex.Message]);
        }
    }

    public async Task<Result<List<LlmTelemetryDto>>> GetRecentTelemetryAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthHeaderAsync();
            var response = await _http.GetAsync("/admin/telemetry/recent", ct);
            if (HandleAuthErrors(response)) return Result<List<LlmTelemetryDto>>.Failure(string.Empty);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<LlmTelemetryDto>>(cancellationToken: ct) ?? new();
                return Result<List<LlmTelemetryDto>>.Success(data);
            }
            return Result<List<LlmTelemetryDto>>.Failure($"Failed to fetch telemetry: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return Result<List<LlmTelemetryDto>>.Failure(_localizer["Error_Network", ex.Message]);
        }
    }

    public async Task<Result<List<DailyTokenUsageDto>>> GetDailyTokenUsageAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthHeaderAsync();
            var response = await _http.GetAsync("/admin/telemetry/daily-tokens", ct);
            if (HandleAuthErrors(response)) return Result<List<DailyTokenUsageDto>>.Failure(string.Empty);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<DailyTokenUsageDto>>(cancellationToken: ct) ?? new();
                return Result<List<DailyTokenUsageDto>>.Success(data);
            }
            return Result<List<DailyTokenUsageDto>>.Failure($"Failed to fetch daily token usage: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return Result<List<DailyTokenUsageDto>>.Failure(_localizer["Error_Network", ex.Message]);
        }
    }
}

