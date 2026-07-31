using DuskOfCoding.Execution.Contracts.DTOs;
using Scalar.AspNetCore;

using DuskOfCoding.ExecutionApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSingleton<SecureCompilationService>();
builder.Services.AddSingleton<SandboxExecutionService>();

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
app.MapPost("/api/executions", async (
    ExecutionRequest request, 
    SecureCompilationService compiler, 
    SandboxExecutionService executor, 
    CancellationToken ct) =>
{
    if (request.TestBundle == null || request.TestBundle.TestCodes.Count() == 0)
    {
        return Results.BadRequest("TestCode is required in the TestBundle.");
    }

    var (isValid, errors, compilation) = compiler.Compile(request.SourceCode, request.TestBundle.TestCodes);

    if (!isValid || compilation == null)
    {
        return Results.Ok(new ExecutionResponse
        {
            SubmissionId = request.SubmissionId,
            Status = "CompilationFailed",
            Compilation = new CompilationResultDto
            {
                Succeeded = false,
                Errors = errors
            }
        });
    }

    // Pass the host's cancellation token down
    var result = await executor.ExecuteAsync(request.SubmissionId, compilation, ct);
    
    return Results.Ok(result);
})
.WithName("ExecuteCode");

app.Run();
