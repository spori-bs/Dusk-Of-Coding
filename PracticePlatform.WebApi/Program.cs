using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticePlatform.Application;
using PracticePlatform.Application.Services;
using PracticePlatform.Infrastructure;
using PracticePlatform.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();

// Register Clean Architecture layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

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

app.UseHttpsRedirection();

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

var submissionsGroup = app.MapGroup("/submissions").WithTags("Submissions");

submissionsGroup.MapPost("/", async ([FromBody] SubmitCodeRequest request, ISubmissionService submissionService, CancellationToken ct) =>
{
    var result = await submissionService.SubmitCodeAsync(request.TaskId, request.SourceCode, request.UserId, ct);
    return Results.Created($"/submissions/{result.Submission.Id}", result);
});

submissionsGroup.MapGet("/{id:guid}", async (Guid id, ISubmissionService submissionService, CancellationToken ct) =>
{
    var submission = await submissionService.GetSubmissionByIdAsync(id, ct);
    return submission is not null ? Results.Ok(submission) : Results.NotFound();
});

app.Run();

// DTOs for endpoints
public record SubmitCodeRequest(Guid TaskId, string SourceCode, Guid? UserId = null);

