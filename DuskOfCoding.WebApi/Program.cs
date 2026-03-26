using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DuskOfCoding.Application;
using DuskOfCoding.Application.Services;
using DuskOfCoding.Infrastructure;
using DuskOfCoding.Infrastructure.Messaging;
using DuskOfCoding.Infrastructure.Persistence;
using DuskOfCoding.WebApi.Hubs;
using DuskOfCoding.WebApi.Services;
using Scalar.AspNetCore;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestHeadersTotalSize = 131072; // 128KB
});

builder.AddServiceDefaults();

// ── Dev certificate trust (Aspire service-to-service) ─────
// Disabling DangerousAcceptAnyServerCertificateValidator using standard Aspire dev-certs.
if (builder.Environment.IsDevelopment())
{
    builder.Services.ConfigureHttpClientDefaults(http =>
    {
        http.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
    });
}

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

// Register Clean Architecture layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

// Register RabbitMQ via Aspire client integration + messaging services
builder.AddRabbitMQClient("messaging");
builder.Services.AddMessagingServices();

// Keycloak JWT Bearer Authentication
builder.Services.AddAuthentication()
       .AddKeycloakJwtBearer("keycloak", realm: "DuskOfCoding", options =>
       {
           options.RequireHttpsMetadata = false;
           if (builder.Environment.IsDevelopment())
           {
               options.BackchannelHttpHandler = new HttpClientHandler
               {
                   ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
               };
           }
           else
           {
               options.BackchannelHttpHandler = new HttpClientHandler(); 
           }
           options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
           {
               RoleClaimType = "roles",
               ValidateAudience = false, // We rely on ValidIssuers in this POC, Audience mapper handles it in prod.
               ValidateIssuer = true,
               ValidIssuers = new[]
               {
                   "http://localhost:8080/realms/DuskOfCoding",
                   "https+http://keycloak/realms/DuskOfCoding",
                   "http://keycloak:8080/realms/DuskOfCoding"
               }
           };
       });
builder.Services.AddAuthorization();

// SignalR for real-time tutor feedback
builder.Services.AddSignalR();

// RabbitMQ → SignalR bridge
builder.Services.AddHostedService<TutorResponseBridge>();

var app = builder.Build();

// Auto-migrate SQLite database on startup (POC only)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHttpsRedirection();
}

// ---- ENDPOINTS ----

var tasksGroup = app.MapGroup("/tasks").WithTags("Tasks").RequireAuthorization();

tasksGroup.MapGet("/", async (ITaskService taskService, CancellationToken ct) =>
{
    var tasks = await taskService.GetAllTasksAsync(ct);
    return Results.Ok(tasks);
});

tasksGroup.MapGet("/{id:guid}", async (Guid id, ITaskService taskService, CancellationToken ct) =>
{
    var task = await taskService.GetTaskByIdAsync(id, ct);
    return task is not null ? Results.Ok(task) : Results.NotFound();
});

tasksGroup.MapPost("/", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async ([FromBody] DuskOfCoding.Application.DTOs.CreateTaskDto dto, ITaskService taskService, CancellationToken ct) =>
{
    var task = await taskService.CreateTaskAsync(dto, ct);
    return Results.Created($"/tasks/{task.Id}", task);
});

tasksGroup.MapPut("/{id:guid}", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (Guid id, [FromBody] DuskOfCoding.Application.DTOs.UpdateTaskDto dto, ITaskService taskService, CancellationToken ct) =>
{
    var task = await taskService.UpdateTaskAsync(id, dto, ct);
    return task is not null ? Results.Ok(task) : Results.NotFound();
});

tasksGroup.MapDelete("/{id:guid}", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (Guid id, ITaskService taskService, CancellationToken ct) =>
{
    var success = await taskService.DeleteTaskAsync(id, ct);
    return success ? Results.NoContent() : Results.NotFound();
});

tasksGroup.MapGet("/{id:guid}/stats", async (Guid id, DuskOfCoding.Infrastructure.Persistence.AppDbContext db, CancellationToken ct) => 
{
    var submissions = await db.Submissions
        .Where(s => s.TaskId == id && s.UserId != null)
        .Select(s => new { s.UserId, s.Status })
        .ToListAsync(ct);

    if (!submissions.Any()) return Results.Ok(new { TaskId = id, TotalUsersAttempted = 0, TotalSubmissions = 0, AverageTries = 0, SuccessRate = 0 });

    var userAttempts = submissions.GroupBy(s => s.UserId);
    var totalUsers = userAttempts.Count();
    var totalTries = submissions.Count;
    var averageTries = (double)totalTries / totalUsers;
    var successfulUsers = userAttempts.Count(g => g.Any(s => s.Status == DuskOfCoding.Domain.Enums.SubmissionStatus.Success));

    return Results.Ok(new { 
        TaskId = id, 
        TotalUsersAttempted = totalUsers, 
        TotalSubmissions = totalTries, 
        AverageTries = Math.Round(averageTries, 1),
        SuccessRate = Math.Round((double)successfulUsers / totalUsers * 100, 1)
    });
});

var adminGroup = app.MapGroup("/admin").WithTags("Admin").RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "admin" });

adminGroup.MapGet("/stats", async (DuskOfCoding.Infrastructure.Persistence.AppDbContext db, CancellationToken ct) => 
{
    var totalStudents = await db.Submissions.Where(s => s.UserId != null).Select(s => s.UserId).Distinct().CountAsync(ct);
    var totalSubmissions = await db.Submissions.CountAsync(ct);
    var totalTasks = await db.Tasks.CountAsync(ct);
    var successfulSubmissions = await db.Submissions.CountAsync(s => s.Status == DuskOfCoding.Domain.Enums.SubmissionStatus.Success, ct);

    double successRate = totalSubmissions > 0 ? ((double)successfulSubmissions / totalSubmissions) * 100 : 0;

    return Results.Ok(new {
        TotalStudents = totalStudents,
        TotalTasks = totalTasks,
        TotalSubmissions = totalSubmissions,
        SuccessRate = Math.Round(successRate, 1)
    });
});

var submissionsGroup = app.MapGroup("/submissions").WithTags("Submissions").RequireAuthorization();

submissionsGroup.MapPost("/", async (
    [FromBody] DuskOfCoding.Application.DTOs.SubmitCodeDto request,
    Microsoft.AspNetCore.Http.HttpContext httpContext,
    ISubmissionService submissionService,
    RabbitMQService rabbitMqService,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    Guid? userId = userIdClaim != null ? Guid.Parse(userIdClaim) : request.UserId;
    // Phase 5: Input validation — reject empty or oversized source code
    if (string.IsNullOrWhiteSpace(request.SourceCode))
        return Results.BadRequest(new { error = "Source code cannot be empty." });

    if (request.SourceCode.Length > 50_000)
        return Results.BadRequest(new { error = "Source code exceeds the maximum allowed length of 50,000 characters." });

    var result = await submissionService.SubmitCodeAsync(request.TaskId, request.SourceCode, userId, ct);

    // Phase 6: Publish to RabbitMQ so the TutorWorker picks up the submission
    try
    {
        var correlationId = Guid.NewGuid();
        var submissionMessage = new SubmissionMessage
        {
            CorrelationId = correlationId,
            TaskId = request.TaskId,
            SubmissionId = result.Submission.Id,
            SourceCode = request.SourceCode,
            Language = "csharp"
        };

        await rabbitMqService.PublishAsync(
            RabbitMQTopology.SubmissionExchange,
            RabbitMQTopology.SubmissionRoutingKey,
            submissionMessage,
            correlationId,
            ct);

        logger.LogInformation(
            "Published submission {SubmissionId} to RabbitMQ with CorrelationId {CorrelationId}",
            result.Submission.Id, correlationId);
    }
    catch (Exception ex)
    {
        // Non-fatal: submission succeeded, but tutor won't respond
        logger.LogWarning(ex, "Failed to publish submission {SubmissionId} to RabbitMQ", result.Submission.Id);
    }

    return Results.Created($"/submissions/{result.Submission.Id}", result);
});

submissionsGroup.MapGet("/{id:guid}", async (Guid id, Microsoft.AspNetCore.Http.HttpContext httpContext, ISubmissionService submissionService, CancellationToken ct) =>
{
    var result = await submissionService.GetSubmissionByIdAsync(id, ct);
    if (result is null) return Results.NotFound();

    var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim != null && result.Submission.UserId.HasValue && result.Submission.UserId.Value.ToString() != userIdClaim)
    {
        return Results.Forbid();
    }

    return Results.Ok(result);
});

var feedbackGroup = app.MapGroup("/feedback").WithTags("Feedback").RequireAuthorization();

feedbackGroup.MapPost("/", async (
    [FromBody] DuskOfCoding.Application.DTOs.CreateFeedbackDto request,
    Microsoft.AspNetCore.Http.HttpContext httpContext,
    IFeedbackService feedbackService,
    CancellationToken ct) =>
{
    var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    if (request.Rating < 0 || request.Rating > 5)
        return Results.BadRequest(new { error = "Rating must be between 0 and 5." });

    if (request.Comment?.Length > 2000)
        return Results.BadRequest(new { error = "Comment cannot exceed 2000 characters." });

    var feedback = await feedbackService.SubmitFeedbackAsync(userId, request, ct);
    return Results.Ok(feedback);
});

feedbackGroup.MapGet("/task/{taskId:guid}/summary", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (
    Guid taskId,
    IFeedbackService feedbackService,
    CancellationToken ct) =>
{
    var summary = await feedbackService.GetTaskFeedbackSummaryAsync(taskId, ct);
    return Results.Ok(summary);
});

feedbackGroup.MapGet("/overview", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")] async (
    IFeedbackService feedbackService,
    CancellationToken ct) =>
{
    var overview = await feedbackService.GetPlatformFeedbackOverviewAsync(ct);
    return Results.Ok(overview);
});

// SignalR hub for real-time tutor responses
app.MapHub<TutorHub>("/hubs/tutor");

app.Run();

