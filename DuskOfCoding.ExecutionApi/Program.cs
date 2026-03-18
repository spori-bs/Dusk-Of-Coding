using DuskOfCoding.Execution.Contracts.DTOs;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();

var app = builder.Build();

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

// Execution API boundary
app.MapPost("/api/executions", (ExecutionRequest request) =>
{
    // For POC scaffolding, just return a simulated successful response
    return Results.Ok(new ExecutionResponse
    {
        SubmissionId = request.SubmissionId,
        Status = "Completed",
        Compilation = new CompilationResultDto { Succeeded = true, Errors = Array.Empty<string>() },
        Tests = new[]
        {
            new TestResultDto { Name = "SimulatedTest", Passed = true, DurationMs = 15 }
        },
        Runtime = new RuntimeMetricsDto { TotalDurationMs = 120 },
        Errors = Array.Empty<string>()
    });
})
.WithName("ExecuteCode");

app.Run();
