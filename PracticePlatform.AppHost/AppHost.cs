var builder = DistributedApplication.CreateBuilder(args);

// Execution API — standalone sandbox service
var executionApi = builder.AddProject<Projects.PracticePlatform_ExecutionApi>("executionapi")
    .WithHttpHealthCheck("/health");

// Web API — main onboarding app, calls Execution API via service discovery
var webApi = builder.AddProject<Projects.PracticePlatform_WebApi>("webapi")
    .WithHttpHealthCheck("/health")
    .WithReference(executionApi);

// Web UI — Blazor Server frontend, calls Web API via service discovery
builder.AddProject<Projects.PracticePlatform_WebUi>("webui")
    .WithHttpHealthCheck("/health")
    .WithReference(webApi);

builder.Build().Run();

