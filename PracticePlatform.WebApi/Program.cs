using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticePlatform.Application;
using PracticePlatform.Application.Services;
using PracticePlatform.Infrastructure;
using PracticePlatform.Infrastructure.Persistence;
using PracticePlatform.WebApi.Hubs;
using PracticePlatform.WebApi.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

var tasksGroup = app.MapGroup("/tasks").WithTags("Tasks");

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

tasksGroup.MapPost("/", async ([FromBody] PracticePlatform.Application.DTOs.CreateTaskDto dto, ITaskService taskService, CancellationToken ct) =>
{
    var task = await taskService.CreateTaskAsync(dto, ct);
    return Results.Created($"/tasks/{task.Id}", task);
});

tasksGroup.MapPut("/{id:guid}", async (Guid id, [FromBody] PracticePlatform.Application.DTOs.UpdateTaskDto dto, ITaskService taskService, CancellationToken ct) =>
{
    var task = await taskService.UpdateTaskAsync(id, dto, ct);
    return task is not null ? Results.Ok(task) : Results.NotFound();
});

tasksGroup.MapDelete("/{id:guid}", async (Guid id, ITaskService taskService, CancellationToken ct) =>
{
    var success = await taskService.DeleteTaskAsync(id, ct);
    return success ? Results.NoContent() : Results.NotFound();
});

var submissionsGroup = app.MapGroup("/submissions").WithTags("Submissions");

submissionsGroup.MapPost("/", async ([FromBody] PracticePlatform.Application.DTOs.SubmitCodeDto request, ISubmissionService submissionService, CancellationToken ct) =>
{
    var result = await submissionService.SubmitCodeAsync(request.TaskId, request.SourceCode, request.UserId, ct);
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

