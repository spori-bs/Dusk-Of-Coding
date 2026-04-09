using DuskOfCoding.Infrastructure;
using DuskOfCoding.Infrastructure.Messaging;
using DuskOfCoding.Infrastructure.Configuration;
using DuskOfCoding.TutorWorker;
using DuskOfCoding.TutorWorker.Resilience;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// ── Infrastructure (DB, Repositories, AI Service) ────────────
builder.Services.AddInfrastructureServices(builder.Configuration.GetConnectionString("DefaultConnection"));

// ── Configuration ──────────────────────────────────────────
builder.Services.Configure<LlmProviderOptions>(
    builder.Configuration.GetSection(LlmProviderOptions.SectionName));

// ── RabbitMQ via Aspire client integration ──────────────────
builder.AddRabbitMQClient("messaging");

// Messaging services (RabbitMQService + TopologyInitializer)
builder.Services.AddMessagingServices();

// ── Execution API HttpClient for MCP tools ──────────────────
builder.Services.AddHttpClient("executionapi", client =>
{
    client.BaseAddress = new Uri("http://executionapi");
});

// ── MCP Server with tools from this assembly ────────────────
builder.Services.AddMcpServer()
    .WithToolsFromAssembly();

// ── LLM Client (OpenAI / Azure OpenAI via configuration) ────
builder.Services.AddConfigurableChatClient();

// ── Polly Resilience for LLM calls ──────────────────────────
builder.Services.AddLlmResilience();

// ── Background workers consuming from RabbitMQ ──────────────
builder.Services.AddHostedService<TutorWorkerService>();
builder.Services.AddHostedService<TestGenerationWorkerService>();

var host = builder.Build();
host.Run();
