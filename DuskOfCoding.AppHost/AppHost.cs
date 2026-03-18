var builder = DistributedApplication.CreateBuilder(args);

// RabbitMQ broker — managed container with management UI
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

// Execution API — standalone sandbox service
var executionApi = builder.AddProject<Projects.DuskOfCoding_ExecutionApi>("executionapi")
    .WithHttpHealthCheck("/health");

// Web API — main onboarding app, calls Execution API via service discovery
var webApi = builder.AddProject<Projects.DuskOfCoding_WebApi>("webapi")
    .WithHttpHealthCheck("/health")
    .WithReference(executionApi)
    .WithReference(messaging)
    .WaitFor(messaging);

// Web UI — Blazor Server frontend, calls Web API via service discovery
builder.AddProject<Projects.DuskOfCoding_WebUi>("webui")
    .WithHttpHealthCheck("/health")
    .WithReference(webApi);

// Tutor Worker — MCP-enabled background worker consuming from RabbitMQ
builder.AddProject<Projects.DuskOfCoding_TutorWorker>("tutorworker")
    .WithReference(messaging)
    .WithReference(executionApi)
    .WaitFor(messaging);

builder.Build().Run();


