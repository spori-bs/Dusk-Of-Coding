var builder = DistributedApplication.CreateBuilder(args);

// RabbitMQ broker — managed container with management UI
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

// Execution API — standalone sandbox service
var executionApi = builder.AddProject<Projects.DuskOfCoding_ExecutionApi>("executionapi")
    .WithHttpHealthCheck("/health");

var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume()
    .WithRealmImport("../KeycloakConfig");

// Web API — main onboarding app, calls Execution API via service discovery
var webApi = builder.AddProject<Projects.DuskOfCoding_WebApi>("webapi")
    .WithHttpHealthCheck("/health")
    .WithReference(executionApi)
    .WithReference(messaging)
    .WithReference(keycloak)
    .WaitFor(messaging)
    .WaitFor(keycloak);

// Web UI — Blazor Server frontend, calls Web API via service discovery
builder.AddProject<Projects.DuskOfCoding_WebUi>("webui")
    .WithHttpHealthCheck("/health")
    .WithReference(webApi)
    .WithReference(keycloak)
    .WaitFor(keycloak);

// Tutor Worker — MCP-enabled background worker consuming from RabbitMQ
builder.AddProject<Projects.DuskOfCoding_TutorWorker>("tutorworker")
    .WithReference(messaging)
    .WithReference(executionApi)
    .WaitFor(messaging);

builder.Build().Run();


