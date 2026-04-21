# 🛠️ Phase 29: Cockpit Observability & Telemetry Dashboard

**Role:** Senior Full-Stack Architect (.NET 10, Blazor Server, Clean Architecture).
**Objective:** Upgrade the `TutorDashboard` (Cockpit) to display dynamic AI provider statuses, introduce a secure LLM Telemetry log view for Admins, and render a zero-JS SVG token usage chart.

**Design System:** Strict adherence to "Obsidian Foundry" (hard edges, `ck-panel`, amber accents, no rounded corners > 4px).

## Execution Steps:

### Step 1: Dynamic AI Provider Status API & UI
- **Target:** WebApi (`Program.cs`) and WebUi (`TutorDashboard.razor`).
- **Action (Backend):** Create a `GET /system/llm-provider` endpoint that reads the configured LLM Provider (e.g., from `IOptions<LlmProviderOptions>`) and returns it.
- **Action (Frontend):** Fetch the provider name via your API client and replace the hardcoded "Gemini AI" string in the system map with this dynamic value.

### Step 2: Backend Telemetry Query & Secure API Endpoint
- **Target:** `ITelemetryService` / `TelemetryService` (and WebApi `Program.cs`).
- **Action:** Create a method: `Task<List<LlmTelemetryDto>> GetRecentTelemetryAsync(int limit = 50)`. Expose this via a new endpoint in WebApi (e.g., `GET /telemetry/recent`).
- **Security:** The endpoint MUST be heavily restricted: `[Authorize(Roles = AppRoles.Admin)]`. Tutors and students should NOT have access.
- **Implementation:** Use Entity Framework Core. Query the `LlmTelemetryLog` table. You MUST explicitly `.Include(t => t.Task)` to fetch the Task Summary/Title without N+1 issues. Order by `CreatedAt` descending.
- **DTO Structure:** The DTO should contain: `Id`, `UserId`, `TaskId`, `TaskTitle`, `ProviderModel`, `PromptTokens`, `CompletionTokens`, `TotalTokens`, `IsSuccess`, and `Timestamp`.

### Step 3: Telemetry Data Aggregation (For Charting)
- **Target:** `TelemetryService` (and WebApi `Program.cs`).
- **Action:** Create a method `Task<List<DailyTokenUsageDto>> GetTokenUsageLast7DaysAsync()`. Expose via a new endpoint in WebApi.
- **Security:** This endpoint MUST also be restricted to Admins: `[Authorize(Roles = AppRoles.Admin)]`.
- **Implementation:** Group successful telemetry logs by Date (last 7 days) and SUM the `TotalTokens`. Return a flat list.

### Step 4: Admin Telemetry UI & SVG Chart
- **Target:** `TutorDashboard.razor`
- **Action 1 (Access Control):** Wrap the new telemetry section in `<AuthorizeView Roles="Admin">`. Tutors should not see this.
- **Action 2 (SVG Chart):** Build a pure HTML/SVG bar chart component (NO JavaScript libraries). Map the `DailyTokenUsageDto` data fetched from the API to vertical SVG rects. Use `--ck-amber` for the bars and display the date/token count on hover or below the bars.
- **Action 3 (Data Table):** Render a highly condensed, tactical data table (using `ck-panel` and small font sizes) displaying the recent 50 logs. Columns: Time, UserID (truncated), Task Title, Model, Tokens, Status (Green check/Red X).

**Constraints:**
- Do not use 3rd party chart libraries.
- Prevent memory leaks: Do not query the entire log table; always use `.Take(limit)`.
- Output only the specific C# classes, EF Core queries, and Razor markup needed to fulfill this phase.