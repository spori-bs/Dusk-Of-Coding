# 🛠️ Phase 29: Cockpit Observability, Telemetry Dashboard, Practice Features, & AI Guardrails

**Role:** Senior Full-Stack Architect (.NET 10, Blazor Server, Clean Architecture).
**Objective:** Upgrade the `TutorDashboard` (Cockpit), introduce a secure standalone LLM Telemetry page for Admins, improve Practice/Task page configurations with robust namespace handling, fix unit test management, and enforce strict AI guardrails against prompt injection.

**Design System:** Strict adherence to "Obsidian Foundry" (hard edges, `ck-panel`, amber accents, no rounded corners > 4px).
**Bilingual Rules:** The application supports dual languages (Hungarian / English). Ensure any new views (including the new Admin Telemetry dashboard) consistently support these bilingual translation patterns or language conventions.

---

## 🛑 Meta-Task: State & Context Tracking
Before beginning the code implementation, you **MUST** create a context document named `phase29-execution-state.md` in the root of the workspace.
- **Instruction:** Use this file to hold the current state. Work sequentially in predefined phases. After you finish each phase, update `phase29-execution-state.md` with:
  1. The completed phase name and a brief summary of what was done.
  2. The exact file paths modified.
  3. Any pending issues, next steps, or specific C#/Blazor notes.
Do **NOT** proceed to the next phase until the previous phase is completed and logged in the state file.

---

## 🚀 Execution Phases

### Phase 1: Tutor Dashboard Enhancement - Feedback Content
- **Target:** `TutorDashboard.razor` (and related backend services/DTOs).
- **Current State:** The dashboard currently displays only the *amount* (count) of feedback given. Also, `TutorDashboard.razor` currently has **hardcoded English strings** and does not use `IStringLocalizer<SharedResource>` — unlike `Practice.razor` which already uses `@L[...]`.
- **Note:** `TutorStatsDto` only carries counts (`TotalStudents`, `TotalTasks`, `TotalSubmissions`, `SuccessRate`). A new endpoint or DTO expansion is required to return feedback content (e.g., from `FeedbackRecord.Summary` and `FeedbackRecord.AiReviewRemarks`).
- **Action:** 
  1. Create a new backend endpoint (or expand the existing stats endpoint) to return recent feedback content.
  2. Modify the Razor UI so that tutors can read the **contents** of the given feedback natively on the dashboard. Use a condensed `ck-panel` tabular list or accordion for readability.
  3. Refactor all hardcoded English strings in `TutorDashboard.razor` to use `@inject IStringLocalizer<SharedResource> L` with `@L["..."]` keys, matching the bilingual pattern already used in `Practice.razor`.

### Phase 2: Dynamic AI Provider Status API & UI
- **Target:** WebApi (`Program.cs`) and WebUi (`TutorDashboard.razor`).
- **Action (Backend):** Create a `GET /system/llm-provider` endpoint that reads the configured LLM Provider (e.g., from `IOptions<LlmProviderOptions>`) and returns its name.
- **⚠️ Architecture Note:** `LlmProviderOptions` lives in `DuskOfCoding.Infrastructure.Configuration`. Ensure WebApi already references Infrastructure, or expose the options class via a shared abstraction to avoid violating Clean Architecture layering.
- **Action (Frontend):** Fetch the provider name via an API client and replace the hardcoded "Gemini AI" string in the system map with this dynamic value.

### Phase 3: Standalone Secure Admin Telemetry Page
- **Target:** **NEW** `ITelemetryService` (in `DuskOfCoding.Application`), **NEW** `TelemetryService` (in `DuskOfCoding.Infrastructure`), WebApi (`Program.cs`), and a **NEW** page (`AdminTelemetry.razor` in WebUi).
- **⚠️ Prerequisites (must be done first):**
  1. `ITelemetryService` and `TelemetryService` **do not exist yet** — they must be created from scratch.
  2. `LlmTelemetryLog` entity currently has **no navigation property** to `TaskDefinition`. Add `public TaskDefinition? Task { get; init; }` to the entity and configure the relationship in `AppDbContext.OnModelCreating`. This will likely require a new EF Core migration.
  3. `DbSet<LlmTelemetryLog> LlmTelemetryLogs` already exists in `AppDbContext` ✅.
- **Action (Backend):** Create two methods: 
  1. `Task<List<LlmTelemetryDto>> GetRecentTelemetryAsync(int limit = 50)` (Query `LlmTelemetryLog` using EF Core `.Include(t => t.Task)`, order by `Timestamp` descending). **Note:** The column is `Timestamp`, not `CreatedAt`.
  2. `Task<List<DailyTokenUsageDto>> GetTokenUsageLast7DaysAsync()` (Group by Date, SUM `TokenCount`). **Note:** The column is `TokenCount`, not `TotalTokens`.
  *(Both must prevent memory leaks by using `.Take(limit)` and be heavily restricted: `[Authorize(Roles = AppRoles.Admin)]`.)*
- **Action (Frontend):** Create `AdminTelemetry.razor`. Ensure it enforces the English/Hungarian translation rules perfectly. 
  - Render a pure HTML/SVG bar chart component (NO JavaScript libraries). Map tokens to vertical SVG rects (`--ck-amber` colored) with a hover effect.
  - Render a highly condensed, tactical data table for the 50 recent logs.
- **Access Control:** This page must ONLY be visible to Admins. Tutors and students should NOT have access. Ensure it's outside `TutorDashboard`.

### Phase 4: Practice & Task Pages - Namespace & Code Generation
- **Target:** `Practice.razor`, `TaskEditor.razor` (note: there is no `Task.razor` — the task configuration page is `TaskEditor.razor`), and AI code generation logic.
- **Action:** 
  - Add a **Namespace Declaration** text field on both pages. We already determine the classname; now we need the namespace.
  - Ensure the generated starter code **and** generated unit tests use this unified, matching namespace.
  - **AI Requirement:** The AI must check for a missing namespace. If the namespace is missing, the AI service MUST provide constructive advice/warnings instructing the user to include it.

### Phase 5: Task Page - Unit Test CRUD Fixes
- **Target:** Task configuration page (`TaskEditor.razor` / unit test editor logic/services).
- **Current Issue:** Saving currently crashes the app if generated unit tests are modified. Additionally, there is no way to delete them.
- **Action:** 
  - Fix the exception / crash occurring upon clicking the 'Save' button.
  - Implement a **Delete** feature for existing unit tests.
  - Allow seamless **modification** of generated unit tests, persisting correctly via the API/EF Core without ID collisions or null references.

### Phase 6: AI-Enhanced Compilation Troubleshooting
- **Target:** `AiReviewService` (`DuskOfCoding.Infrastructure\Services\AiReviewService.cs`) and its interface `IAIReviewService` (`DuskOfCoding.Domain\Interfaces\IAIReviewService.cs`). Also involves the prompt templates in `DuskOfCoding.WebApi\Prompts\`.
- **Action:** When unit tests fail to build/compile, the AI must actively parse the build errors. It should return a clear, educational explanation directly to the student detailing *why* the unit tests could not compile.

### Phase 7: AI Guardrails against Prompt Injection
- **Target:** `AiReviewService` (`DuskOfCoding.Infrastructure\Services\AiReviewService.cs`) — specifically the system prompt construction and user-input processing in `EnrichFeedbackAsync`. Also the prompt templates in `DuskOfCoding.WebApi\Prompts\`.
- **Action:** Introduce an AI guardrail to detect prompt injections (e.g., A student writes instructions instead of code, trying to extract the system prompt, ignore preceding instructions, or bypass the assignment).
- **Feature:** If prompt injection is detected, the AI must halt evaluation and respond with a **highly amusing / funny warning message** explicitly stating that injecting additional prompts or hacking the system is a violation of the coding project's goals.

---
**Strict Constraints Summarized:**
- Do not use 3rd party charting libraries (pure SVG only).
- Work strictly phase-by-phase! Update the context file `phase29-execution-state.md` each time!
- Output only the specific C# classes, EF Core queries, and Razor markup needed to fulfill the phase.