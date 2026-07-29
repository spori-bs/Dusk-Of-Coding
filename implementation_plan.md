# Agentic Architecture Migration using Semantic Kernel

This plan outlines the migration from `Microsoft.Extensions.AI` (`IChatClient` and `AITool`) to **Microsoft Semantic Kernel (v1.78.0)** and its **Agent Framework** (`ChatCompletionAgent`).

In addition to supporting cloud providers (OpenAI, Azure OpenAI, Google Gemini), we will add native support for **Local LLMs** (e.g., Ollama, LM Studio, vLLM) by allowing the configuration of custom OpenAI-compatible endpoints.

## User Review Required

> [!IMPORTANT]
> **Local LLM Support**: We will add a `CustomEndpoint` field to `LlmProviderOptions`. Setting `"Provider": "Local"` or `"Provider": "Custom"` will configure Semantic Kernel's OpenAI connector to point to this endpoint (e.g., `http://localhost:11434/v1` for Ollama). This allows seamless local execution.
>
> **Gemini Compatibility**: Gemini will continue to be run through Semantic Kernel's OpenAI connector using its official OpenAI-compatible endpoint.

## Open Questions

> [!NOTE]
> 1. **Co-existence of MCP and SK Plugins**: Currently, the application registers an MCP Server (`builder.Services.AddMcpServer().WithToolsFromAssembly()`). We will add the `[KernelFunction]` attribute next to `[McpServerTool]` in the tool definitions. This allows Semantic Kernel to consume the exact same tools natively as SK Plugins while keeping the MCP server capability active. Do you prefer this dual-attribute approach, or should we migrate completely to SK Plugins and retire MCP?

## Proposed Changes

We will introduce `Microsoft.SemanticKernel` and `Microsoft.SemanticKernel.Agents.Core` packages to `DuskOfCoding.Infrastructure` and `DuskOfCoding.TutorWorker`.

---

### [Component: Infrastructure & Dependency Injection]

#### [MODIFY] [LlmProviderOptions.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.Infrastructure/Configuration/LlmProviderOptions.cs)
- Add new properties to support local/custom endpoints:
  ```csharp
  public string? CustomEndpoint { get; set; }
  public string? ApiKey { get; set; }
  ```

#### [MODIFY] [InfrastructureAiExtensions.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.Infrastructure/InfrastructureAiExtensions.cs)
- Register `Kernel` in Dependency Injection.
- Introduce `AddConfigurableKernel(this IServiceCollection services)` that constructs a Semantic Kernel `KernelBuilder` based on `LlmProviderOptions`.
- Add support for:
  - **OpenAI**: Native `AddOpenAIChatCompletion`.
  - **Azure OpenAI**: Native `AddAzureOpenAIChatCompletion`.
  - **Gemini**: `AddOpenAIChatCompletion` pointing to `https://generativelanguage.googleapis.com/v1beta/openai/` with GeminiApiKey.
  - **Local/Custom**: `AddOpenAIChatCompletion` pointing to `CustomEndpoint` with custom `HttpClient`.

#### [MODIFY] [DependencyInjection.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.Infrastructure/DependencyInjection.cs)
- Call `services.AddConfigurableKernel();` to register the SK Kernel in DI.

---

### [Component: MCP Tools / SK Plugins]

#### [MODIFY] [AnalyzeCodeTool.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.TutorWorker/McpTools/AnalyzeCodeTool.cs)
- Import `Microsoft.SemanticKernel`.
- Annotate `AnalyzeCode` with `[KernelFunction]`.

#### [MODIFY] [ExecuteCustomTestTool.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.TutorWorker/McpTools/ExecuteCustomTestTool.cs)
- Import `Microsoft.SemanticKernel`.
- Annotate `ExecuteCustomTest` with `[KernelFunction]`.

---

### [Component: Tutor Worker Services]

#### [MODIFY] [TutorWorkerService.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.TutorWorker/TutorWorkerService.cs)
- Refactor the Socratic tutoring logic to construct and invoke a `ChatCompletionAgent` using the registered `Kernel`.
- Register the tools (`AnalyzeCodeTool` and `ExecuteCustomTestTool`) as SK Plugins on the tutor agent's cloned kernel.
- Enable automatic function calling (`FunctionChoiceBehavior.Auto()`) so the tutor agent can run checks or execute tests automatically during the conversation.
- Collect the response from the agent and publish feedback.

#### [MODIFY] [TestGenerationWorkerService.cs](file:///d:/Repos/Dusk-Of-Coding/DuskOfCoding.TutorWorker/TestGenerationWorkerService.cs)
- Refactor the test generator to utilize a `ChatCompletionAgent` instance.
- Construct the agent using the `Kernel`, configure it with the test generation instructions, and invoke it to get the test suite JSON response.
- Update the telemetry logging to extract usage data from the Semantic Kernel model response metadata.

---

## Verification Plan

### Automated Tests
- Run `dotnet build` to ensure successful compilation.
- Ensure the unit tests compile and run properly.

### Manual Verification
- Run the system locally and submit a task through the UI to verify that the Socratic Tutor agent successfully responds (and calls internal plugins if needed).
- Generate a test suite for a coding task to verify the test generation agent works and produces valid JSON.
