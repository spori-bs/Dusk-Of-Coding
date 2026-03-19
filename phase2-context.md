# Phase 2 Context — Execution Engine, Persistence & Domain Hardening

> **Last updated**: 2026-03-19
> **Source**: `phase2-master-promt.md`

---

## Architecture Overview

Building on the existing AI-Tutor Platform, Phase 2 replaces the stubbed Execution API with a **Roslyn-based sandbox** that compiles student code against a **whitelisted assembly set**, generates xUnit tests on the fly, executes them in an isolated `AssemblyLoadContext`, and returns pass/fail results. Feedback is persisted to the database and the domain model is hardened.

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
            GATE["Syntax Analysis Gate"]
            ROSLYN["Roslyn Compiler (Whitelisted Refs)"]
            ALC["AssemblyLoadContext (Isolated)"]
            RUNNER["xUnit Test Runner"]
        end

        RMQ["RabbitMQ Broker"]
        DB[("SQLite DB")]
    end

    UI -->|HTTP| API
    UI <-->|WebSocket| HUB
    API -->|Publish submission| RMQ
    API -->|Persist feedback| DB
    RMQ -->|Consume| Worker
    Worker -->|MCP Tool Call| MCP
    MCP -->|ExecuteCustomTest| EXECAPI
    EXECAPI --> GATE
    GATE -->|Safe code| ROSLYN
    ROSLYN -->|Compiled assembly| ALC
    ALC --> RUNNER
    RUNNER -->|Test results| EXECAPI
    Worker -->|Publish response| RMQ
    RMQ -->|Consume response| BRIDGE
    BRIDGE -->|Push via CorrelationId| HUB

    style RMQ fill:#ff6b35,color:#fff
    style LLM fill:#6c5ce7,color:#fff
    style MCP fill:#00b894,color:#fff
    style GATE fill:#e17055,color:#fff
    style ROSLYN fill:#0984e3,color:#fff
    style ALC fill:#fdcb6e,color:#000
    style DB fill:#0984e3,color:#fff
```

---

## Security Model

| Layer | Mechanism | What it Prevents |
|-------|-----------|-----------------|
| 1. Compile-time whitelist | Only whitelisted `MetadataReference`s in Roslyn compilation | File I/O, network, process spawning, P/Invoke |
| 2. Syntax analysis | Reject `unsafe`, pointers, `DllImport`, **Reflection**, `goto`, broad `catch` | Sandbox bypass, native execution, swallowed timeouts |
| 3a. Loop rewriting | Inject `ThrowIfCancellationRequested()` into every loop body | Infinite loops (`while(true){}`) |
| 3b. Recursion depth | Inject call-depth counter into every method (limit: 200) | Infinite recursion / fatal `StackOverflowException` |
| 4. Runtime protection | 5s timeout + `GetAllocatedBytesForCurrentThread` (50 MB) + ALC unload | Hangs, memory bombs, assembly leaks (⚠️ In-process thread leak risk accepted) |
| 5. Concurrency limit | `SemaphoreSlim` (configurable, default: 5) | Resource exhaustion from concurrent submissions |

---

## 📈 Phase Tracking

| Phase | Description | Status |
|-------|-------------|--------|
| 1 | Domain Model Hardening (enum, mutability, rename) | ✅ Completed |
| 2 | Feedback Persistence (EF entity, migration) | ✅ Completed |
| 3 | Real Execution Engine (Roslyn sandbox, test gen, xUnit runner) | ✅ Completed |
| 4 | Integration Wiring (connect engine to services & UI) | ✅ Completed |
| 5 | End-to-End Verification | ⬜ Not Started |

---

## Technical Constraints

- **Runtime**: .NET 10
- **Execution**: Roslyn in-process + AssemblyLoadContext (NO Docker/Podman)
- **Messaging**: Official `RabbitMQ.Client` (no MassTransit)
- **Background Logic**: Standard `BackgroundService`
- **Resiliency**: Polly v8+ Strategy-based API
- **ORM**: Entity Framework Core (SQLite for dev, PostgreSQL for prod)
- **Prefer**: `Microsoft.Extensions.*` standard libraries
- **Aspire**: Wire everything in the AppHost
- **Deployment targets**: On-premise (VM) and Azure

---

## Key Decisions Log

| Date | Decision |
|------|----------|
| 2026-03-19 | Phase 2 work initiated — assessed current project gaps |
| 2026-03-19 | Rejected Podman — doesn't work inside containers (Azure, K8s) |
| 2026-03-19 | Chosen Roslyn compile-time whitelist as primary security boundary |
| 2026-03-19 | Updated Sandbox Rules: Block Reflection & swallow-catch to patch security holes |
| 2026-03-19 | Tests run via pure Reflection `Invoke` (xUnit util requires disk I/O) |
| 2026-03-19 | Thread memory tracking via `GC.GetAllocatedBytesForCurrentThread()` |
| 2026-03-19 | Feedback to be persisted as `FeedbackRecord` EF entity with JSON-serialized lists |
| 2026-03-19 | Phase 1 completed: SubmissionStatus enum added, mutability fixed, migrations applied |
| 2026-03-19 | Phase 2 completed: FeedbackRecord persisted via EF Core. Added UserId to WebApi for security and aggregate endpoint. |
| 2026-03-19 | Phase 3 completed: Roslyn sandbox implemented. Added SecureCompilationService with SandboxSyntaxAnalyzer and SafetyRewriter, and SandboxExecutionService with ALC, 5s timeout, and reflection-based test execution. |
| 2026-03-19 | Phase 4 completed: Execution engine re-wired to use TestCode in execution requests. Polly resilience added to the HttpClient. SocraticTutorPrompt strictly constrained from rendering direct code solutions and given context about compiler/test logging. |

---

## Resumption Point (2026-03-19)

**Phase 4 is COMPLETE.** 

### To resume
1. Read this `phase2-context.md` to understand current status
2. Begin with **Phase 5 (End-to-End Verification)** tasks from `phase2-master-promt.md`
3. Update this file at the end of every response
