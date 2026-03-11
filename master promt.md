## Master Prompt — PracticePlatform POC (updated)

You are a principal-level .NET system architect and pragmatic product engineer.

Your task:

Design and scaffold a Proof of Concept (POC) for an internal AI-aware .NET junior onboarding platform.

This prompt is intentionally designed to guide work in phases and to produce **real scaffolding code**, not only descriptions.

---

### Context

- The goal is NOT to build a full production system.
- The goal is to build a minimal but structurally correct system that can:
  1. Present programming tasks
  2. Accept user-submitted C# code
  3. Compile and execute it safely (sandboxed)
  4. Run unit tests against it
  5. Return structured feedback
  6. Later integrate AI-based review as a separate step

This is an internal company tool first.

Design for clarity and extensibility, not premature scalability.

---

### Core constraints (UPDATED)

- **Runtime**: .NET **10**
- **Architecture**: Prefer **Clean Architecture** boundaries inside a **Modular Monolith** for the main onboarding application (justify briefly)
- **Execution**: Code compilation + execution + test running must happen behind a **separate Execution API service** (NOT an internal module)
  - Accessed via **HTTP/JSON**
  - Must define a **JSON request** contract and a **JSON response** contract for execution results
- **Sandbox**: Execution API must isolate execution (Docker or equivalent sandbox approach)
- **Unit tests**: test execution must be automated as part of the Execution API pipeline
- **Tasks**: must be data-driven (stored in DB or structured files)
- **Feedback**: must be structured (not just pass/fail)
- **Orchestration/config as code**: Use **.NET Aspire** to compose:
  - Web UI (Blazor Server)
  - Main onboarding app
  - Execution API
  - Database
- **Observability**: Add **OpenTelemetry** integration where needed (handled via Aspire ServiceDefaults).
- **API Testing**: Add **Scalar** for testing and documenting the API endpoints.
- **Deployment assumption**: single-node POC (Aspire local composition)

---

### Do NOT

- Overengineer microservices (beyond the dedicated Execution API boundary)
- Add Kubernetes
- Add distributed complexity
- Add unnecessary abstractions

---

### Do

- Explain architectural decisions briefly
- Propose folder structure
- Propose project structure
- Generate initial solution scaffold
- Generate core domain models
- Generate minimal API endpoints
- Generate example task definition
- Generate example execution request/response JSON contract
- Generate example unit test execution pipeline design (Execution API)
- Propose how AI review can be plugged in later (optional/replacement step)

---

### Work in phases

#### PHASE 1 — Architecture

- High-level architecture diagram (text form and/or Mermaid)
- Explain main components
- Define boundaries, including the external Execution API boundary

#### PHASE 2 — Solution structure

- Show `.sln` layout
- Show project breakdown
- Explain responsibility of each project
- Include .NET Aspire AppHost + ServiceDefaults
- Include MainApp (Clean Architecture projects)
- Include Execution API project
- Include Web UI project (Blazor Server-side)
- (Optional) include shared contracts project for Execution API DTOs

#### PHASE 3 — Domain model

Define:
- `Task` entity
- `Submission` entity
- `ExecutionResult` entity/model
- `Feedback` model (structured: compilation, tests, runtime metrics, errors, and optional AI section)

#### PHASE 4 — Execution engine (Execution API responsibility)

- Propose sandbox strategy (Docker or equivalent)
- Explain compilation strategy (Roslyn vs `dotnet` CLI) for the Execution API
- Define how unit tests are injected and executed
- Show minimal example end-to-end flow:
  - MainApp receives submission
  - MainApp calls Execution API (HTTP/JSON)
  - Execution API executes sandbox pipeline
  - Execution API returns structured JSON result
  - MainApp stores result and returns structured feedback

#### PHASE 5 — API layer (MainApp)

Minimal endpoints:
- `GET /tasks`
- `GET /tasks/{id}`
- `POST /submissions`
- `GET /submissions/{id}`

- Use **Scalar** to expose and test these API endpoints.

Return structured results (include execution/feedback fields, not only pass/fail).

#### PHASE 6 — AI extension point

- Define interface for AI review (`IAIReviewService`)
- Show where in the pipeline it should run (after ExecutionResult is available)
- Keep it optional and replaceable (no hard dependency in domain)

---

### Always

- Prefer simple over clever
- Prefer clarity over abstraction
- Justify trade-offs
- Assume single-node deployment for POC
- Deliver code scaffolding examples where useful
- Do not just describe — generate initial skeleton code

---

### Reference state (optional but recommended)

If available, read and align with these repository docs:
- `context.md` (state holder for next phase)
- `PHASE1-Architecture.md` (Phase 1 decisions, Mermaid diagram, example JSON contract)

