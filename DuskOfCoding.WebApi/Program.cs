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

builder.AddServiceDefaults();

// ── Dev certificate trust (Aspire service-to-service) ─────
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
           options.BackchannelHttpHandler = new HttpClientHandler
           {
               ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
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

tasksGroup.MapPost("/", async ([FromBody] DuskOfCoding.Application.DTOs.CreateTaskDto dto, ITaskService taskService, CancellationToken ct) =>
{
    var task = await taskService.CreateTaskAsync(dto, ct);
    return Results.Created($"/tasks/{task.Id}", task);
});

tasksGroup.MapPut("/{id:guid}", async (Guid id, [FromBody] DuskOfCoding.Application.DTOs.UpdateTaskDto dto, ITaskService taskService, CancellationToken ct) =>
{
    var task = await taskService.UpdateTaskAsync(id, dto, ct);
    return task is not null ? Results.Ok(task) : Results.NotFound();
});

tasksGroup.MapDelete("/{id:guid}", async (Guid id, ITaskService taskService, CancellationToken ct) =>
{
    var success = await taskService.DeleteTaskAsync(id, ct);
    return success ? Results.NoContent() : Results.NotFound();
});

var submissionsGroup = app.MapGroup("/submissions").WithTags("Submissions").RequireAuthorization();

submissionsGroup.MapPost("/", async (
    [FromBody] DuskOfCoding.Application.DTOs.SubmitCodeDto request,
    ISubmissionService submissionService,
    RabbitMQService rabbitMqService,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    // Phase 5: Input validation — reject empty or oversized source code
    if (string.IsNullOrWhiteSpace(request.SourceCode))
        return Results.BadRequest(new { error = "Source code cannot be empty." });

    if (request.SourceCode.Length > 50_000)
        return Results.BadRequest(new { error = "Source code exceeds the maximum allowed length of 50,000 characters." });

    var result = await submissionService.SubmitCodeAsync(request.TaskId, request.SourceCode, request.UserId, ct);

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

submissionsGroup.MapGet("/{id:guid}", async (Guid id, ISubmissionService submissionService, CancellationToken ct) =>
{
    var submission = await submissionService.GetSubmissionByIdAsync(id, ct);
    return submission is not null ? Results.Ok(submission) : Results.NotFound();
});

// SignalR hub for real-time tutor responses
app.MapHub<TutorHub>("/hubs/tutor");

app.Run();

