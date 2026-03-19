# Rebranding Context — Dusk of Coding

> **Last updated**: 2026-03-18  
> **Target**: `Dusk of Coding` at `dusk-of-coding.eu`  

---

## 🏗 Full Architecture Overview

The PracticePlatform has evolved from a POC into an **Interactive AI-Tutor Platform** with **Bilingual Localization**.

- **Target runtime**: .NET 10
- **Orchestrator**: .NET Aspire (`PracticePlatform.AppHost` and `PracticePlatform.ServiceDefaults`)
- **Main App**: Modular Monolith with Clean Architecture (`Domain`, `Application`, `Infrastructure`, `WebApi`, `WebUi` [Blazor Server])
- **Execution Engine**: Isolated Docker Sandbox API (`PracticePlatform.ExecutionApi`, `PracticePlatform.Execution.Contracts`)
- **Messaging & Resiliency**: RabbitMQ + Polly (Retry/Timeout/CircuitBreaker) + SignalR
- **AI Integration**: MCP-enabled `PracticePlatform.TutorWorker` processing background LLM reviews (supports OpenAI and Azure OpenAI).
- **Localization**: Bilingual (English & Hungarian default), using `IStringLocalizer` and `.resx` files across layers.

## 📁 Existing Project Structure

The solution file is `PracticePlatform.slnx`. Here are the current projects that will need namespace and file renaming:

| Project | Role |
|---------|------|
| `PracticePlatform.AppHost` | Aspire orchestrator |
| `PracticePlatform.ServiceDefaults` | Shared Aspire defaults, OpenTelemetry |
| `PracticePlatform.Domain` | Entities, interfaces, models |
| `PracticePlatform.Application` | Services (SubmissionService, TaskService), `Messages.resx` |
| `PracticePlatform.Infrastructure` | EF Core, HTTP engine, DI Registration |
| `PracticePlatform.WebApi` | Minimal API endpoints, Hubs, `Messages.resx` |
| `PracticePlatform.WebUi` | Blazor Server frontend, `App.resx` |
| `PracticePlatform.ExecutionApi` | Sandboxed code execution (Docker) |
| `PracticePlatform.Execution.Contracts` | Shared DTOs for execution |
| `PracticePlatform.TutorWorker` | Background service, MCP tools, LLM calls |

## 🌍 Key Localization Considerations

The rebranding must touch localization files because the UI and backend messages refer to the system name and brand concepts in multiple languages.
- **Current Languages**: English (`en`), Hungarian (`hu`).
- **Resource Files**: 
  - `PracticePlatform.WebUi/Resources/App.resx` (and `.hu.resx`)
  - `PracticePlatform.WebApi/Resources/Messages.resx` (and `.hu.resx`)
  - `PracticePlatform.Application/Resources/Messages.resx` (and `.hu.resx`)
- Changes should alter platform instances in both English ("PracticePlatform") and Hungarian (if translated or conceptualized).

## 🧠 Technical Nuances for the Rebrand
1. **Docker Compose/Container Names**: The `ExecutionApi` relies on specific container names/tags in `Infrastructure` or `AppHost`. Check strings carefully.
2. **Aspire Component Names**: e.g., `builder.AddProject<Projects.PracticePlatform_WebUi>("webui")`.
3. **Environment Variables & AppSettings**: `TutorWorker/appsettings.json` and `WebApi/appsettings.json` may contain platform references.
4. **RabbitMQ Topic/Queue Names**: Connections and queues used by TutorWorker and WebApi.

This context file represents the complete snapshot of the system going into the rebranding phase. Use this alongside `rebrand.md` to ensure no components are missed.
