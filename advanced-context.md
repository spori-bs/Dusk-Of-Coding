# Advanced Context — AI-Tutor Platform

> **Last updated**: 2026-03-12  
> **Branch**: `feature/ai-tutor-platform`

---

## Architecture Overview

The PracticePlatform is being evolved from a **Modular Monolith POC** into an **Interactive AI-Tutor Platform** using:

- **RabbitMQ** (official `RabbitMQ.Client`) for async messaging
- **MCP** (Model Context Protocol) for LLM ↔ Roslyn/Sandbox tool calling
- **Polly** (v8+ Strategy API) for retry, circuit breaker, timeout, and fallback
- **SignalR** for real-time Blazor UI updates

```mermaid
flowchart LR
    subgraph Clients
        UI["Blazor WebUI"]
    end

    subgraph Aspire[".NET Aspire Orchestration"]
        subgraph WebApi["Web API + SignalR Hub"]
            API["Minimal API"]
            HUB["SignalR Hub"]
            BRIDGE["RabbitMQ → SignalR Bridge"]
        end

        subgraph Worker["TutorWorker (BackgroundService)"]
            MCP["MCP Server (Tools)"]
            LLM["LLM Client + Polly"]
        end

        subgraph ExecService["Execution API"]
            EXECAPI["HTTP/JSON Endpoint"]
        end

        RMQ["RabbitMQ Broker"]
    end

    UI -->|HTTP| API
    UI <-->|WebSocket| HUB
    API -->|Publish submission| RMQ
    RMQ -->|Consume| Worker
    Worker -->|MCP Tool Call| MCP
    MCP -->|AnalyzeCode| MCP
    MCP -->|ExecuteCustomTest| EXECAPI
    Worker -->|Publish response| RMQ
    RMQ -->|Consume response| BRIDGE
    BRIDGE -->|Push via CorrelationId| HUB

    style RMQ fill:#ff6b35,color:#fff
    style LLM fill:#6c5ce7,color:#fff
    style MCP fill:#00b894,color:#fff
```

---

## Phase Tracking

| Phase | Description | Status |
|-------|-------------|--------|
| 0 | Branch & Tracking Setup | ✅ Done |
| 1 | Native RabbitMQ & Polly Connection Strategy | ✅ Done |
| 2 | MCP-Enabled Worker with Fallback | ✅ Done |
| 3 | Resilient LLM Loop | ✅ Done |
| 4 | Blazor WebUI & SignalR Bridge | ✅ Done |
| 5 | Sandboxing & Safety | 🔲 Not started |
| 6 | End-to-End Verification | 🔲 Not started |

---

## Existing Project Structure

| Project | Role |
|---------|------|
| `PracticePlatform.AppHost` | Aspire orchestrator |
| `PracticePlatform.ServiceDefaults` | Shared Aspire defaults |
| `PracticePlatform.Domain` | Entities, interfaces, models |
| `PracticePlatform.Application` | Services (SubmissionService, TaskService) |
| `PracticePlatform.Infrastructure` | EF Core, HTTP execution engine, AI review |
| `PracticePlatform.WebApi` | Minimal API endpoints |
| `PracticePlatform.WebUi` | Blazor Server frontend |
| `PracticePlatform.ExecutionApi` | Sandboxed code execution (stub) |
| `PracticePlatform.Execution.Contracts` | Shared DTOs for execution |

---

## Technical Constraints

- **Runtime**: .NET 10
- **Messaging**: Official `RabbitMQ.Client` (no MassTransit)
- **Background Logic**: Standard `BackgroundService`
- **Resiliency**: Polly v8+ Strategy-based API
- **AI Protocol**: MCP for Tool Calling
- **Prefer**: `Microsoft.Extensions.*` standard libraries
- **Aspire**: Wire everything in the AppHost

---

## Key Decisions Log

| Date | Decision |
|------|----------|
| 2026-03-12 | Created `feature/ai-tutor-platform` branch from `main` |
| 2026-03-12 | Phase 1 before Phase 2 (dependency order) |
| 2026-03-12 | Phase 1 done: RabbitMQ.Client 7.2.1, Polly 10.4.0, Aspire.RabbitMQ.Client 13.1.2 |
| 2026-03-12 | Phase 2 done: ModelContextProtocol 1.1.0, TutorWorker BackgroundService with MCP tools |
| 2026-03-12 | Phase 3 done: IChatClient with switchable OpenAI/AzureOpenAI, Polly retry+timeout, Socratic Tutor prompt |
| 2026-03-12 | Phase 4 done: SignalR TutorHub, RabbitMQ→SignalR bridge, TutorTerminal Blazor component |

---

## Resumption Point (2026-03-12 18:41)

**Next up: Phase 5 — Sandboxing & Safety**, then Phase 6 — End-to-End Verification.

### Phase 5 scope
- Add 5s execution timeouts to MCP tools
- HTML sanitize tutor responses before rendering
- Add input length limits
- Validate SourceCode is not empty/too long

### Phase 6 scope
- Full end-to-end flow test via Aspire Dashboard
- Verify: Submit code → RabbitMQ → TutorWorker (LLM) → RabbitMQ → SignalR → TutorTerminal

### Git state
- **Branch**: `feature/ai-tutor-platform`
- **Last commit**: `60fa6ad` — Phase 4 (Blazor WebUI & SignalR Bridge)
- **Build**: ✅ 0 errors, 0 warnings (all 10 projects)

### LLM provider config needed
- Set `LlmProvider:OpenAIApiKey` or switch to `AzureOpenAI` in `TutorWorker/appsettings.json`
- No API key is configured yet — LLM calls will hit the Polly fallback path

