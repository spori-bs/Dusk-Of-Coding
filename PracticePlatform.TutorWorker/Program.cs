using PracticePlatform.Infrastructure;
using PracticePlatform.Infrastructure.Messaging;
using PracticePlatform.TutorWorker;
using PracticePlatform.TutorWorker.Configuration;
using PracticePlatform.TutorWorker.LlmClient;
using PracticePlatform.TutorWorker.Resilience;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

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

// ── Background worker consuming from RabbitMQ ───────────────
builder.Services.AddHostedService<TutorWorkerService>();

var host = builder.Build();
host.Run();
