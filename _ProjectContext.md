# 🪐 Dusk of Coding - Project Context

This document provides a comprehensive overview of the **PracticePlatform** (Dusk of Coding) codebase for AI assistance (Optimized for Gemini 1.5 Pro and Claude 3.5 Sonnet / Opus).

---

## 🏗️ System Architecture
The project follows a **Clean Architecture** approach integrated with **.NET Aspire**.

### 📁 Project Structure
- **`DuskOfCoding.AppHost`**: Orchestration (Aspire). Manages dependencies like Redis, RabbitMQ, and project references.
- **`DuskOfCoding.WebUi`**: Blazor Server frontend.
    - Uses **Monaco Editor** (via `BlazorMonaco`) for code editing.
    - Industrial "Obsididan Foundry" aesthetic (Amber/Dark).
- **`DuskOfCoding.WebApi`**: Core API.
    - Uses **Minimal APIs**.
    - Handles Task management, user progress, and SignalR hubs.
- **`DuskOfCoding.TutorWorker`**: Background worker.
    - Consumes RabbitMQ messages.
    - Orchestrates AI feedback (Socratic Tutor) and test generation.
- **`DuskOfCoding.Application`**: Business logic, DTOs, and Service interfaces.
- **`DuskOfCoding.Infrastructure`**: Persistence (`AppDbContext` on SQLite) and Messaging implementations.
- **`DuskOfCoding.Domain`**: Core entities (`Task`, `Submission`, `TaskTest`, etc.) and constants.
- **`DuskOfCoding.Execution.Contracts`**: Shared message records for RabbitMQ.

---

## 🛠️ Technology Stack
- **Framework:** .NET 10 / ASP.NET Core
- **Frontend:** Blazor Server + Bootstrap + Custom CSS
- **Database:** Entity Framework Core + SQLite
- **Messaging:** RabbitMQ (MassTransit)
- **Real-time:** SignalR
- **AI Integration:** `Microsoft.Extensions.AI` (Connecting to Gemini/OpenAI)
- **Editor:** Monaco Editor

---

## 🚩 Current Milestone: Phase 22 (Hardening)
We have completed the basic UI for a multi-file **Task Editor** (Phase 21). We are now moving into **Phase 22: Test Engineering Hardening**.

### Key Objectives:
1.  **Async AI Pipeline:** Move the "Generate Test Suite" logic from a synchronous HTTP call to an event-driven background process (WebUi -> WebApi -> RabbitMQ -> TutorWorker -> SignalR -> WebUi).
2.  **Monaco Editor Stability:** Implement state-guards (`_isEditorReady`) to prevent race conditions when switching files in the sidebar.
3.  **Real-Time Feedback:** Use SignalR to display Toast notifications for long-running AI tasks.

---

## 📝 Coding Guidelines
- **Naming:** PascalCase for Classes/Methods, camelCase for local variables, `_camelCase` for private fields.
- **Async:** Always use `CancellationToken` and `Task.Run` where appropriate.
- **Minimal APIs:** Use `TypedResults` or `Results` in `Program.cs` for endpoints.
- **Blazor:** Prefer `[Inject]` or `@inject` over manual service resolution. Use `StateHasChanged()` sparingly.

---

## 📂 Key Files
- `DuskOfCoding.WebUi/Components/Pages/TaskEditor.razor`: The primary complex UI component.
- `DuskOfCoding.WebApi/Program.cs`: The central hub for API routing and service registration.
- `phase22-test-engineering-hardening.md`: The active roadmap.
