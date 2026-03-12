using PracticePlatform.Infrastructure;
using PracticePlatform.Infrastructure.Messaging;
using PracticePlatform.TutorWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// RabbitMQ via Aspire client integration
builder.AddRabbitMQClient("messaging");

// Messaging services (RabbitMQService + TopologyInitializer)
builder.Services.AddMessagingServices();

// Execution API HttpClient for MCP tools
builder.Services.AddHttpClient("executionapi", client =>
{
    client.BaseAddress = new Uri("http://executionapi");
});

// MCP Server with tools from this assembly
builder.Services.AddMcpServer()
    .WithToolsFromAssembly();

// Background worker that consumes from RabbitMQ
builder.Services.AddHostedService<TutorWorkerService>();

var host = builder.Build();
host.Run();
