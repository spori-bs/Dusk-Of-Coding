# Phase 14 Master Prompt: Role Refactoring and AI Polish

## Objective
Implement role-based access control (RBAC) refactoring, replace dummy AI behavior with a real implementation in Hungarian, and enforce C# coding guidelines across the "PracticePlatform" (DuskOfCoding) project.

## Context
- **Roles**: Currently `admin` and `student`. The `admin` role must be renamed to `tutor` (technical) and `Oktató` (display).
- **Language**: Hungarian (`hu`) is the target language for all UI and AI feedback.
- **AI**: `MockAIReviewService` in `DuskOfCoding.Infrastructure` is a placeholder and should be replaced with a real implementation.
- **Guidelines**: C# "one class per file", file-scoped namespaces, and proper naming conventions.

## Instructions

### 1. Role Refactoring (Renaming Admin to Tutor)
- Rename the `admin` role to `tutor` in the following locations:
  - `KeycloakConfig\realm.json`: Update the role name and any users associated with it.
  - `DuskOfCoding.WebApi\Program.cs`: Update authorization policies and `Authorize` attributes.
  - `DuskOfCoding.WebUi\Components\Layout\NavMenu.razor`: Update the `Roles="admin"` attribute to `Roles="tutor"`.
  - `DuskOfCoding.WebUi\Components\Pages\AdminDashboard.razor`: Rename the file to `TutorDashboard.razor` and update the route to `/tutor`.
  - `DuskOfCoding.WebUi\Services\ApiClient.cs`: Update any calls that reference "admin" endpoints.
- Ensure the `tutor` role has access to both the "Practice" page and the "Tutor Dashboard" (formerly Admin Dashboard).
- Fix UI visibility: Privileged functions (like the "Tasks" button) must be visible to users with the `tutor` role.

### 2. AI Service Implementation (Real Behavior)
- Replace `MockAIReviewService.cs` in `DuskOfCoding.Infrastructure` with a real implementation.
- Use `Microsoft.Extensions.AI` (IChatClient) if possible, or integrate with an OpenAI-compatible API.
- All AI responses must be in **Hungarian**.
- The AI should provide Socratic-style feedback (guiding the student towards the solution rather than providing it directly).
- Update `TutorWorkerService.cs` in `DuskOfCoding.TutorWorker` to ensure its feedback is also in Hungarian and follows the same Socratic principles.
- Add necessary configuration (API keys, model IDs) to `appsettings.json`.

### 3. C# Coding Guidelines & Cleanup
- Enforce the "one class per file" rule across the entire solution.
- **Crucial Cleanup**: Extract the DTO classes (e.g., `TaskDto`, `CreateTaskDto`, etc.) from `DuskOfCoding.WebUi\Services\ApiClient.cs` into a new `DuskOfCoding.WebUi.DTOs` namespace (each in its own file).
- Use file-scoped namespaces throughout the project.
- Ensure private fields use the `_` prefix (e.g., `_httpClient`).

### 4. Localization Polish
- Review `DuskOfCoding.WebUi\Resources\SharedResource.hu.resx`.
- Replace any English fallback or "dummy" messages with professional Hungarian equivalents.
- Ensure the AI responses do not contain any "placeholder" text.

## Definition of Done
- A user with the `tutor` role can log in and see the "Tasks" and "Dashboard" links.
- Submitting code results in a helpful, Hungarian-language AI review.
- The project structure is clean, with one class per file and no nested DTOs in services.
- All "admin" terminology is replaced with "tutor" or "Oktató".
