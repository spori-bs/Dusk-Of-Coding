## PracticePlatform – State Holder (for next phase prompts)

### Purpose

This file is the **single source of truth for the current POC state** so work can continue in another session without re-discovery. It is written for an AI prompt context (English), while code/concept names remain in their original form (e.g., `Task`, `Submission`, `ExecutionResult`, `Feedback`).

---

### Repo / workspace

- **Workspace path**: `d:\Repos\PracticePlatform`
- **GitHub repo**: `https://github.com/spori-bs/PracticePlatform`
- **Branch**: `main`
- **Current tracked files**:
  - `PHASE1-Architecture.md` (Hungarian text, English concept/code names)
  - `README.md`
  - `.gitignore`

---

### Phase 1 (Architecture) – decisions locked in

- **Target runtime**: **.NET 10**
- **Main system style**: **Modular Monolith** with **Clean Architecture** boundaries inside the main app.
- **Execution is NOT internal**:
  - Code execution + unit test execution runs in a **separate Execution API service** (HTTP/JSON).
  - The main app calls it via an HTTP client behind `ICodeExecutionEngine` (conceptually).
- **Orchestration / config-as-code**: **.NET Aspire**
  - Aspire coordinates:
    - Web UI (Blazor Server)
    - Main onboarding app
    - Execution API
    - Database
  - **OpenTelemetry** is integrated via Aspire ServiceDefaults across all components.
  - **Structured logging** using **Serilog**.
  - The **Aspire Dashboard must contain all info from telemetry, logging, and all infrastructure components (UI, APIs, DB)**.
- **AI review**:
  - Optional and replaceable, behind `IAIReviewService`
  - POC uses a no-op/stub implementation

High-level components:
- **MainApp**: Web API + Application + Domain + Infrastructure
- **Execution API**: accepts JSON request, runs sandboxed compile/test, returns JSON execution result
- **DB**: stores Tasks and Submissions (and persisted execution results/feedback)

Reference doc: `PHASE1-Architecture.md` (includes Mermaid HL diagram + example JSON contracts).

---

### Execution API JSON contract (current direction)

We already documented example shapes in `PHASE1-Architecture.md`. For Phase 2/3 scaffolding, the contract should be formalized as DTOs.

- **Request**: includes at minimum `taskId`, `submissionId`, `sourceCode`, `language`, `testBundle`, `limits`.
- **Response**: includes at minimum `submissionId`, `status`, `compilation`, `tests[]`, `runtime`, `errors[]`.

Important: The Execution API is a **hard boundary**; the main app must treat it as an external service (even if hosted locally via Aspire).

---

### Constraints (POC scope)

- **Single-node** deployment for POC (Aspire composes local resources).
- **No microservices overengineering** beyond the single external Execution API.
- **No Kubernetes**.
- **Tasks are data-driven** (DB or structured files). POC can start with structured files and later persist to DB.
- **Feedback must be structured** (not only pass/fail).
- **Sandbox**: Docker or equivalent. Execution API owns sandboxing.

---

### Phase 2 – what to build next (solution structure)

Goal of Phase 2: create the **.sln layout + project breakdown**, wire with Aspire, and produce a minimal scaffold that compiles.

#### Required solution-level components

- **Aspire AppHost project**
  - Defines resources for:
    - Web UI (Blazor Server)
    - Main App (Web API)
    - Execution API (Web API)
    - Database (likely Postgres/SQL Server container, or SQLite for simplest POC)
  - Wires configuration:
    - MainApp → ExecutionApi base URL
    - MainApp/ExecutionApi → DB connection string(s)

- **ServiceDefaults project** (Aspire standard)
  - Shared service defaults (health, telemetry, etc.) kept minimal for POC.

#### Main application (Modular Monolith) projects

Recommended (simple, clean boundaries):
- `PracticePlatform.Domain`
- `PracticePlatform.Application`
- `PracticePlatform.Infrastructure`
- `PracticePlatform.WebApi` (Minimal API, featuring **Scalar** for OpenAPI testing)

Execution boundary in main app:
- `ICodeExecutionEngine` implemented as `HttpCodeExecutionEngine` in `Infrastructure`.

#### Execution API projects

Recommended:
- `PracticePlatform.ExecutionApi` (Minimal API, featuring **Scalar** for OpenAPI testing)
- (Optional) `PracticePlatform.Execution.Contracts` shared DTOs for request/response JSON
  - If used: referenced by both main app and execution API to keep the wire contract aligned.

#### Persistence / DB

POC-friendly options (choose one in Phase 2 scaffold):
- **Option A (simplest dev)**: SQLite + EF Core in the main app only.
- **Option B (Aspire-friendly container)**: Postgres/SQL Server container, EF Core in main app.

Minimum persisted data:
- Tasks (or tasks from files + metadata in DB)
- Submissions + ExecutionResult summary + Feedback

---

### Phase 2 – acceptance criteria (definition of done)

- A single `.sln` with Aspire + all projects created.
- Aspire AppHost can start MainApp + ExecutionApi + DB (even if DB is stubbed initially).
- MainApp can call ExecutionApi via configured base URL (no business logic yet required).
- Repositories compile with .NET 10 target (or the chosen TFM aligned to your installed SDK).
- `PHASE1-Architecture.md` remains the authoritative Phase 1 reference.

---

### Implementation notes / defaults to use unless overridden

- **Naming**:
  - Solution name: `PracticePlatform`
  - Main app: `PracticePlatform.WebApi`
  - Execution service: `PracticePlatform.ExecutionApi`
- **Transport**: HTTP/JSON only (no gRPC unless requested).
- **Auth**: none for POC (can be added later).
- **AI**: keep as interface + no-op implementation.

---

---

### Phase tracking and Roadmap

- **Phase 1**: Architecture [DONE]
- **Phase 2**: Solution structure [DONE]
- **Phase 3**: Domain model [DONE]
- **Phase 4**: Execution engine boundary [DONE]
- **Phase 5**: API layer (MainApp) [DONE]
- **Phase 6**: Aspire Integration (UI/API/DB routing) [DONE]
- **Phase 7**: Persistence (EFCore) [DONE]
- **Phase 8**: Task Management UI [PENDING]
- **Phase 9**: Health Checks [PENDING]
- **Phase 10**: Code Playground UI [PENDING]
- **Phase 11**: Language Selector & Roslyn Integration [PENDING]
- **Phase 12**: AI extension point [PENDING]

---

### Phase 8 – what to build next (Task Management UI)

Goal of Phase 8: Create pages in the Web UI where administrators can maintain (create, edit, delete) practice task examples. These pages must use the API to persist changes to the database.

Requirements:
- Ensure the API has the necessary CRUD endpoints for `Tasks`.
- Add `ApiClient` methods in the Web UI to interact with these endpoints.
- Build Web UI pages for managing tasks.

### Next prompt (copy/paste for Phase 8 continuation)

“Implement Phase 8: Create the Task Management UI. Add necessary CRUD operations for tasks to the API, and build pages in the Web UI to list, create, edit, and delete them.”

