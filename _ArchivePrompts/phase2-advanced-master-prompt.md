# Master Prompt: AI-Tutor Platform (MCP + RabbitMQ + Blazor + Polly)

You are a Principal Software Architect specializing in .NET, Distributed Systems, and AI Integration.

## Objective

Evolve the "PracticePlatform" into an **Interactive AI-Tutor Platform**. The system must allow an LLM to act as a Socratic Tutor that can write and execute code/tests in a sandbox via MCP. **Resiliency and graceful failure are mandatory.**

## Core Architectural Shift

1. **Communication**: Official **RabbitMQ.Client** (No MassTransit).
2. **Intelligence**: **Model Context Protocol (MCP)** for Tool Calling (Roslyn/Sandbox).
3. **UI**: Blazor WebUI monitoring RabbitMQ via a SignalR bridge.
4. **Resiliency**: Use **Polly** for retry policies, circuit breakers, and fallbacks.

## Technical Constraints

* **Runtime**: .NET 10
* **Messaging**: Official **RabbitMQ.Client**.
* **Background Logic**: Standard .NET **`BackgroundService`**.
* **Resiliency Library**: **Polly** (v8+ Strategy-based API).
* **AI Protocol**: **MCP** for Tool Calling.

---

## The `context.md` Protocol

You **must** maintain a file named `context.md` in the root. Update it at the end of every response and read it at the start.

---

## Work in Phases

### PHASE 1: Native RabbitMQ & Polly Connection Strategy

* Implement `RabbitMQService` using `RabbitMQ.Client`.
* **Polly Integration**: Define a `ResiliencePipeline` for the RabbitMQ connection. It must handle transient network drops when connecting to the broker.
* Define **Exchange/Queue Topology**: `SubmissionExchange`, `TutorInteractionsQueue`, `ResponseExchange`.
* Implement **CorrelationID** logic.

### PHASE 2: The MCP-Enabled Worker with Fallback

* Create a `TutorWorker : BackgroundService`.
* **MCP Server**: Wrap the existing **Roslyn** and **Sandbox** logic.
* Expose Tools: `AnalyzeCode(string code)` and `ExecuteCustomTest(string studentCode, string testCode)`.

### PHASE 3: Resilient LLM Loop

* Define the **Socratic Tutor** system prompt.
* **Polly for AI**: Implement a Resilience Pipeline for the LLM API calls:
* **Retry Policy**: 2-3 retries for transient errors (HTTP 5xx, 429 Rate Limits with `Retry-After`).
* **Timeout Policy**: Overall timeout for the LLM "thought" process.
* **Fallback Policy**: If retries fail or tokens are exhausted, execute a fallback that sends a "Kindly try again later" message to the `ResponseExchange`.



### PHASE 4: Blazor WebUI & SignalR Bridge

* Create a SignalR Hub in the API to push RabbitMQ events to the Blazor client using the `CorrelationId`.
* Build the **"Tutor Terminal"** UI component.

### PHASE 5: Sandboxing & Safety

* Implement 5-second execution timeouts for MCP tools.
* Limit the total number of MCP tool calls allowed per single student submission.

### PHASE 6: End-to-End Verification

* Test the "AI Down" scenario: Simulate an LLM failure and verify the student receives the "Safety Message" + raw Roslyn diagnostics.
* Test the "Broker Down" scenario: Verify Polly's connection retry logic.

---

## Deliverables per Phase

1. **Architecture Update**: Mermaid diagram showing the flow including Polly's intervention points.
2. **Scaffold Code**: Using `RabbitMQ.Client`, `BackgroundService`, and `Polly`.
3. **Resiliency Logic**: Show the `ResiliencePipeline` configuration for the AI client.
4. **Updated `advanced-context.md**`.

---

## Always

* **Prefer standard libraries** (Microsoft.Extensions.*).
* **Aspire Integration**: Wire everything in the `AppHost`.
* **No Stuck Requests**: Every message in the queue must eventually result in a UI update, even if it's an error message.

---

### Current Status

* **Base App**: Clean Architecture POC with Roslyn/Sandbox is ready.
* **Next Task**: Transition to **Phase 1 (RabbitMQ + Polly Connection)** and **Phase 2 (MCP Worker)**.

**Please start by acknowledging the architecture and generating the updated `advanced-context.md` and Phase 1 code.**


