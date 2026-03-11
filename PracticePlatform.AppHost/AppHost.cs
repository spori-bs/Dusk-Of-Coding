var builder = DistributedApplication.CreateBuilder(args);

// Execution API — standalone sandbox service
var executionApi = builder.AddProject<Projects.PracticePlatform_ExecutionApi>("executionapi");

// Web API — main onboarding app, calls Execution API via service discovery
var webApi = builder.AddProject<Projects.PracticePlatform_WebApi>("webapi")
    .WithReference(executionApi);

// Web UI — Blazor Server frontend, calls Web API via service discovery
builder.AddProject<Projects.PracticePlatform_WebUi>("webui")
    .WithReference(webApi);

builder.Build().Run();

