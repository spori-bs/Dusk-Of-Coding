# Phase 29 — Execution State

## Status: 🔄 IN PROGRESS — Phase 5

---

## ✅ Completed Phases

### Phase 1: Tutor Dashboard Enhancement - Feedback Content
**Status:** ✅ COMPLETE

### Phase 2: Dynamic AI Provider Status API & UI
**Status:** ✅ COMPLETE

### Phase 3: Standalone Secure Admin Telemetry Page
**Status:** ✅ COMPLETE  
**Summary:** Added `TaskDefinition? Task` nav property to `LlmTelemetryLog`, configured the EF relationship in `AppDbContext`, and generated migration `AddLlmTelemetryTaskNavigation`. Created `ITelemetryService` (Application) and `TelemetryService` (Infrastructure) from scratch. Added project reference `Application → Infrastructure.csproj` and pinned `Microsoft.CodeAnalysis.Common 5.3.0` to resolve NuGet version conflict. Added two Admin-only endpoints (`GET /admin/telemetry/recent`, `GET /admin/telemetry/daily-tokens`). Created `AdminTelemetry.razor` with a pure SVG bar chart (rendered via `MarkupString` to avoid Razor `<text>` tag collision) and a condensed logs table. Added Admin-only nav link in `NavMenu.razor`. Localization keys added to all three resx files.

**Files Modified/Created:**
- `DuskOfCoding.Domain/Entities/LlmTelemetryLog.cs` — Added `TaskDefinition? Task` nav property
- `DuskOfCoding.Infrastructure/Persistence/AppDbContext.cs` — Configured `LlmTelemetryLog` relationship
- `DuskOfCoding.Infrastructure/Migrations/[timestamp]_AddLlmTelemetryTaskNavigation.cs` — New migration
- `DuskOfCoding.Application/DTOs/TelemetryDtos.cs` — New: `LlmTelemetryDto`, `DailyTokenUsageDto`
- `DuskOfCoding.Application/Services/ITelemetryService.cs` — New interface
- `DuskOfCoding.Infrastructure/Services/TelemetryService.cs` — New implementation
- `DuskOfCoding.Infrastructure/DependencyInjection.cs` — Registered `ITelemetryService`
- `DuskOfCoding.Infrastructure/DuskOfCoding.Infrastructure.csproj` — Added Application ref + CodeAnalysis.Common pin
- `DuskOfCoding.WebApi/Program.cs` — Two new admin telemetry endpoints
- `DuskOfCoding.WebUi/DTOs/LlmTelemetryDto.cs` — New WebUi DTO
- `DuskOfCoding.WebUi/DTOs/DailyTokenUsageDto.cs` — New WebUi DTO
- `DuskOfCoding.WebUi/Services/ApiClient.cs` — Added `GetRecentTelemetryAsync`, `GetDailyTokenUsageAsync`
- `DuskOfCoding.WebUi/Resources/SharedResource.*.resx` — AdminTelemetry keys (all 3)
- `DuskOfCoding.WebUi/Components/Layout/NavMenu.razor` — Admin-only Telemetry nav link
- `DuskOfCoding.WebUi/Components/Pages/AdminTelemetry.razor` — New Admin-only page

**Notes:**
- SVG `<text>` collides with Razor's `<text>` tag; resolved by building SVG as `MarkupString` in C#.
- Infrastructure.csproj had no Application reference before; added now with CodeAnalysis conflict pin.

### Phase 4: Practice & TaskEditor Pages — Namespace Field
**Status:** ✅ COMPLETE  
**Summary:** Added `Namespace` property end-to-end: `TaskDefinition` entity → EF migration → Application DTOs → TaskService → `GenerateTestSuiteCommand` message → `GenerateTestsPrompt` (wraps generated tests in the configured namespace) → `AiReviewService` (detects missing namespace in student submission and injects a bilingual warning into the AI prompt) → WebUi DTOs → `TaskEditor.razor` (new Namespace field) → `Practice.razor` (namespace-aware starter code template + inline amber warning banner on submit if namespace is missing).

**Files Modified/Created:**
- `DuskOfCoding.Domain/Entities/TaskDefinition.cs` — Added `Namespace` property
- `DuskOfCoding.Infrastructure/Migrations/[timestamp]_AddNamespaceToTaskDefinition.cs` — New migration
- `DuskOfCoding.Application/DTOs/TaskDtos.cs` — Added `Namespace` to `CreateTaskDto`, `UpdateTaskDto`
- `DuskOfCoding.Application/Services/TaskService.cs` — Wired `Namespace` in Create/Update
- `DuskOfCoding.Infrastructure/Messaging/Messages.cs` — Added `Namespace` to `GenerateTestSuiteCommand`
- `DuskOfCoding.WebApi/Program.cs` — Passes `Namespace` when publishing command
- `DuskOfCoding.TutorWorker/Prompts/GenerateTestsPrompt.cs` — Accepts namespace; generates tests wrapped in it
- `DuskOfCoding.TutorWorker/TestGenerationWorkerService.cs` — Passes `command.Namespace` to prompt
- `DuskOfCoding.Infrastructure/Services/AiReviewService.cs` — Namespace missing detection + bilingual warning injected into AI user message
- `DuskOfCoding.WebUi/DTOs/TaskDto.cs`, `CreateTaskDto.cs`, `UpdateTaskDto.cs` — Added `Namespace`
- `DuskOfCoding.WebUi/Resources/SharedResource.*.resx` — `TaskEditor_FieldNamespace`, `Practice_NamespaceWarning` keys (all 3)
- `DuskOfCoding.WebUi/Components/Pages/TaskEditor.razor` — Namespace text field + wired to model/save/reload
- `DuskOfCoding.WebUi/Components/Pages/Practice.razor` — `BuildStarterCode()` helper, `_namespaceWarning` field, amber warning banner

---

## ⏳ Pending Phases

### Phase 5: TaskEditor — Unit Test CRUD Fixes
- Fix save crash when tests are modified
- Add per-test delete button (already exists in UI — need to verify backend)

### Phase 6: AI Compilation Troubleshooting
- Target: `AiReviewService.cs`, `IAIReviewService.cs`, `Prompts/`

### Phase 7: AI Guardrails (Prompt Injection)
- Target: `AiReviewService.EnrichFeedbackAsync`

---

## ✅ Completed Phases

### Phase 1: Tutor Dashboard Enhancement - Feedback Content
**Status:** ✅ COMPLETE  
**Summary:** Added recent feedback comment content display to the TutorDashboard. Introduced `RecentFeedbackItemDto` DTO, a new `GetRecentFeedbackAsync` service method, and a `GET /feedback/recent-comments` API endpoint. Refactored all hardcoded English strings in `TutorDashboard.razor` to use `IStringLocalizer<SharedResource>`. Added a new `ck-panel` tabular section (`// 03`) displaying comment text, rating, type, task name, and date.

**Files Modified:**
- `DuskOfCoding.Application/DTOs/FeedbackDtos.cs` — Added `RecentFeedbackItemDto`
- `DuskOfCoding.Application/Services/IFeedbackService.cs` — Added `GetRecentFeedbackAsync`
- `DuskOfCoding.Application/Services/FeedbackService.cs` — Implemented `GetRecentFeedbackAsync`
- `DuskOfCoding.WebApi/Program.cs` — Added `GET /feedback/recent-comments` endpoint
- `DuskOfCoding.WebUi/DTOs/RecentFeedbackItemDto.cs` — New file
- `DuskOfCoding.WebUi/Services/ApiClient.cs` — Added `GetRecentFeedbackCommentsAsync`
- `DuskOfCoding.WebUi/Resources/SharedResource.resx` — Added TutorDashboard keys (HU default)
- `DuskOfCoding.WebUi/Resources/SharedResource.hu.resx` — Added TutorDashboard keys (HU)
- `DuskOfCoding.WebUi/Resources/SharedResource.en.resx` — Added TutorDashboard keys (EN)
- `DuskOfCoding.WebUi/Components/Pages/TutorDashboard.razor` — Full localization + new feedback section

### Phase 2: Dynamic AI Provider Status API & UI
**Status:** ✅ COMPLETE  
**Summary:** Created `GET /system/llm-provider` endpoint in WebApi that reads `IOptions<LlmProviderOptions>` (already registered) and returns `{ provider, modelId }`. Added `LlmProviderStatusDto` DTO and `GetLlmProviderAsync()` in ApiClient. Added `// 04 System Map` section to TutorDashboard showing provider name and model ID fetched dynamically. No hardcoded provider names remain in the UI. Localization keys added to all three resx files.

**Files Modified:**
- `DuskOfCoding.WebApi/Program.cs` — Added `GET /system/llm-provider` endpoint
- `DuskOfCoding.WebUi/DTOs/LlmProviderStatusDto.cs` — New file
- `DuskOfCoding.WebUi/Services/ApiClient.cs` — Added `GetLlmProviderAsync`
- `DuskOfCoding.WebUi/Resources/SharedResource.resx` — Added SystemMap keys (HU default)
- `DuskOfCoding.WebUi/Resources/SharedResource.hu.resx` — Added SystemMap keys (HU)
- `DuskOfCoding.WebUi/Resources/SharedResource.en.resx` — Added SystemMap keys (EN)
- `DuskOfCoding.WebUi/Components/Pages/TutorDashboard.razor` — Added `// 04` System Map section + `_llmProvider` field + fetch
- `DuskOfCoding.WebUi/Resources/SharedResource.hu.resx` — Added TutorDashboard keys (HU)
- `DuskOfCoding.WebUi/Resources/SharedResource.en.resx` — Added TutorDashboard keys (EN)
- `DuskOfCoding.WebUi/Components/Pages/TutorDashboard.razor` — Full localization + new feedback section

**Notes:**
- The `RecentFeedbackItemDto` is a flat projection: TaskTitle + Comment + Rating + FeedbackType + CreatedAt.
- Backend uses `IFeedbackRepository.GetAllAsync()` + in-memory sort/take(20). Acceptable for POC scale.
- The `dusk-glass-panel` CSS class was kept for the error state (pre-existing style, not changed).
- No DB migration needed for Phase 1 — only read queries against existing data.

---

## ⏳ Pending Phases

### Phase 2: Dynamic AI Provider Status API & UI
- Create `GET /system/llm-provider` in WebApi
- Read from `IOptions<LlmProviderOptions>` (already registered in WebApi)
- Replace hardcoded provider string in TutorDashboard with dynamic fetch
- **Note:** `LlmProviderOptions` is in Infrastructure but WebApi already references Infrastructure ✅

### Phase 3: Standalone Secure Admin Telemetry Page
- Create `ITelemetryService` + `TelemetryService` from scratch
- Add `TaskDefinition?` nav property to `LlmTelemetryLog` entity + EF migration
- Create `AdminTelemetry.razor` (Admin-only)
- Pure SVG bar chart for token usage, condensed table for recent logs
- **Columns to use:** `Timestamp` (not `CreatedAt`), `TokenCount` (not `TotalTokens`)

### Phase 4: Practice & TaskEditor Pages — Namespace Field
- Target: `Practice.razor`, `TaskEditor.razor`
- Add Namespace Declaration text field
- Wire through AI code generation

### Phase 5: TaskEditor — Unit Test CRUD Fixes
- Fix save crash when tests are modified
- Add delete button per unit test

### Phase 6: AI Compilation Troubleshooting
- Target: `AiReviewService.cs` + `IAIReviewService.cs` + Prompts
- Parse build errors, return educational explanation

### Phase 7: AI Guardrails (Prompt Injection)
- Target: `AiReviewService.EnrichFeedbackAsync`
- Detect injections, return funny warning
