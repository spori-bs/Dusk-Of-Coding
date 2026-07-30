# Agentic Architecture & Real-Time AI Tutor Migration Plan

This plan outlines the migration from `Microsoft.Extensions.AI` (`IChatClient`) to **Microsoft Semantic Kernel (v1.78.0)** and its **Agent Framework** (`ChatCompletionAgent`), transforming **Dusk of Coding** into an interactive, agentic learning platform.

In addition to cloud LLM providers (OpenAI, Azure OpenAI, Google Gemini), we are adding native support for **Local LLMs** (e.g. Ollama, LM Studio, vLLM) and introducing **Real-Time Interactive AI Tutoring** so students receive immediate micro-guidance (e.g. memory/allocation advice like `StringBuilder`) while coding *before* submitting full code.

---

## User Review Required

> [!IMPORTANT]
> **Real-Time Interactive AI Assistance**:
> Currently, AI guidance is only triggered *after* a student submits code and full execution runs. We are extending the architecture to support **interactive real-time learning**:
> 1. **Bi-directional Tutor Chat**: Students can ask questions directly to the Socratic Tutor inside the `TutorTerminal` widget at any time.
> 2. **Explicit Lightbulb 💡 Hint Trigger**: When a student clicks the **Lightbulb 💡** action button next to Monaco Editor or inside `TutorTerminal`, a ReAct agent inspects draft Roslyn diagnostics & code structure to give immediate Socratic hints (e.g., *"What if you used StringBuilder here to reduce memory allocations?"*) without spoiling solutions. **This is explicitly triggered by the user via the Lightbulb interaction (never automatically)** to prevent unnecessary LLM calls and avoid disturbing the student while typing.

> [!IMPORTANT]
> **Local LLM Support**:
> We will add `CustomEndpoint` and `ApiKey` properties to `LlmProviderOptions`. Setting `"Provider": "Local"` or `"Provider": "Custom"` configures Semantic Kernel's OpenAI connector to point to local endpoints (e.g., `http://localhost:11434/v1` for Ollama).

---

## Open Questions & Design Decisions

> [!NOTE]
> 1. **Trigger Mechanism (Resolved)**: Hinting is strictly triggered on explicit **Lightbulb 💡** action button interactions by the user (not automatically) to avoid noise and save API calls.
> 2. **MCP & SK Plugins Dual Annotation**: We will annotate tools with both `[KernelFunction]` and `[McpServerTool]`. This allows Semantic Kernel to consume the tools natively as SK Plugins while keeping the external MCP server capability active.

---

## Proposed Changes

We will introduce `Microsoft.SemanticKernel` and `Microsoft.SemanticKernel.Agents.Core` packages to `DuskOfCoding.Infrastructure` and `DuskOfCoding.TutorWorker`.

---

### [Component 1: Infrastructure & LLM Configuration]

#### [MODIFY] [LlmProviderOptions.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.Infrastructure/Configuration/LlmProviderOptions.cs)
- Add new properties to support custom and local LLM endpoints:
  ```csharp
  public string? CustomEndpoint { get; set; }
  public string? ApiKey { get; set; }
  ```

#### [MODIFY] [InfrastructureAiExtensions.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.Infrastructure/InfrastructureAiExtensions.cs)
- Register `Kernel` in Dependency Injection via `AddConfigurableKernel(this IServiceCollection services)`.
- Support OpenAI, Azure OpenAI, Gemini, and Local/Custom OpenAI-compatible endpoints via custom `HttpClient`.

#### [MODIFY] [DependencyInjection.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.Infrastructure/DependencyInjection.cs)
- Register Semantic Kernel in DI via `services.AddConfigurableKernel()`.

---

### [Component 2: SK Plugins / Tool Registry]

#### [MODIFY] [AnalyzeCodeTool.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.TutorWorker/McpTools/AnalyzeCodeTool.cs)
- Add `[KernelFunction]` attribute next to `[McpServerTool]` for Roslyn AST & syntax analysis.

#### [MODIFY] [ExecuteCustomTestTool.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.TutorWorker/McpTools/ExecuteCustomTestTool.cs)
- Add `[KernelFunction]` attribute for sandboxed test execution.

---

### [Component 3: Tutor Worker & Interactive Agent System]

#### [NEW] [InteractiveHintMessage.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.Infrastructure/Messaging/InteractiveHintMessage.cs)
- Message contract for real-time draft queries & interactive tutor questions:
  ```csharp
  public sealed class InteractiveHintRequestMessage
  {
      public Guid TaskId { get; set; }
      public string UserId { get; set; } = string.Empty;
      public string DraftSourceCode { get; set; } = string.Empty;
      public string? UserQuestion { get; set; }
      public string PreferredLanguage { get; set; } = "en";
  }
  ```

#### [MODIFY] [TutorWorkerService.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.TutorWorker/TutorWorkerService.cs)
- Refactor evaluation logic to use a `ChatCompletionAgent` with `FunctionChoiceBehavior.Auto()`.
- Add a new subscription for `tutor.interactive.requests` queue to handle real-time chat & draft code hinting.
- Agent uses `AnalyzeCodeTool` to inspect student draft code and deliver targeted Socratic advice.

#### [MODIFY] [SocraticTutorPrompt.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.TutorWorker/Prompts/SocraticTutorPrompt.cs)
- Enhance prompts to support both full submission reviews and real-time micro-hints (e.g. suggesting `StringBuilder`, algorithm improvements, or allocation reductions without providing direct solutions).

---

### [Component 4: Test Generation Agent (Self-Verifying Loop)]

#### [MODIFY] [TestGenerationWorkerService.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.TutorWorker/TestGenerationWorkerService.cs)
- Upgrade test generation to a **Self-Verifying Agent Loop**:
  1. Agent generates candidate xUnit test suite JSON.
  2. Agent calls `ExecuteCustomTestTool` to verify the generated tests compile and run against the expected `ExpectedClassName` SUT.
  3. If compilation fails or tests error out, agent self-corrects the test code and re-runs validation before saving to MariaDB.

---

### [Component 5: Core WebApi & Real-Time SignalR Bridge]

#### [MODIFY] [TutorHub.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.WebApi/Hubs/TutorHub.cs)
- Add SignalR client methods:
  - `AskTutor(string taskId, string draftCode, string question)`
  - `RequestDraftHint(string taskId, string draftCode)`
- Publish interactive hint commands to RabbitMQ for processing by `TutorWorkerService`.

#### [MODIFY] [RabbitMQTopology.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.Infrastructure/Messaging/RabbitMQTopology.cs)
- Define `InteractiveTutorQueue` and `InteractiveTutorRoutingKey`.

---

### [Component 6: WebUi Real-Time Practice HUD]

#### [MODIFY] [TutorTerminal.razor](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.WebUi/Components/TutorTerminal.razor)
- Add an interactive chat input field allowing students to type questions directly to the Tutor.
- Add a **"💡 Socratic Hint"** action button.
- Support real-time streaming of interactive hint messages.

#### [MODIFY] [Practice.razor](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.WebUi/Components/Pages/Practice.razor)
- Connect Monaco Editor paste events and hint requests to `TutorTerminal`.

---

## Verification Plan

### Automated Tests
- Run `dotnet build DuskOfCoding.slnx` to verify clean compilation across all projects.
- Run all unit and integration tests.

### Manual Verification
1. **Interactive Real-Time Hinting**:
   - Open Practice page, paste code with inefficient string concatenation inside a loop.
   - Click "💡 Socratic Hint" or ask "How can I optimize memory?".
   - Verify the Tutor Agent responds with a Socratic tip suggesting `StringBuilder` without supplying full solution code.
2. **Autonomous Tool Evaluation**:
   - Submit code and verify `TutorWorkerService` agent invokes Roslyn tools dynamically before responding.
3. **Self-Verifying Test Suite Generation**:
   - Trigger test generation in Admin Cockpit and verify the agent executes tests against the sandbox prior to persistence.
