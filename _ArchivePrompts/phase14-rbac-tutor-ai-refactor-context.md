# Phase 14 Context: Minor Fixes and AI Enhancements

This document provides context for the Phase 14 modifications.

## Role Renaming
- **Old Role**: `admin`
- **New Technical Role**: `tutor`
- **Hungarian Display Name**: `Oktató`
- **Scope**:
  - Keycloak: `realm.json`
  - WebApi: Authorization policies and `Authorize` attributes in `Program.cs`.
  - WebUi: `NavMenu.razor`, `LoginDisplay.razor`, `ApiClient.cs`, and `AdminDashboard.razor` (should probably be renamed to `TutorDashboard.razor`).
  - Access: The `tutor` role must have access to both the "Practice" area (currently for all authorized users) and the "Manage Tasks" / "Dashboard" areas.

## Language and Localization
- **Target Language**: Hungarian (`hu`).
- **Resource Files**: `DuskOfCoding.WebUi\Resources\SharedResource.hu.resx`.
- **AI Requirements**:
  - All AI review remarks must be in Hungarian.
  - The AI should provide helpful, Socratic-style feedback (leading the student to the answer, not giving it).
  - Replace English fallback messages in `MockAIReviewService.cs` and `TutorWorkerService.cs`.

## AI Implementation Details
- **Current Mock**: `MockAIReviewService.cs` in `DuskOfCoding.Infrastructure`.
- **Existing Real implementation**: `TutorWorkerService.cs` in `DuskOfCoding.TutorWorker` uses `Microsoft.Extensions.AI` and `IChatClient`.
- **Goal**: Implement a "real" version of `IAIReviewService` or ensure the initial feedback is also generated via an LLM.

## Coding Guidelines
- **Rule**: One class per file.
- **Cleanup**: Extract DTOs from `ApiClient.cs` into a dedicated `DTOs` folder/namespace in `DuskOfCoding.WebUi`.
- **Namespace Style**: File-scoped namespaces (e.g., `namespace DuskOfCoding.Namespace;`).
- **Naming**: PascalCase for public members, camelCase with `_` for private fields.

## Discovered Improvements
- **Dashboard Renaming**: `AdminDashboard.razor` -> `TutorDashboard.razor` (and corresponding `/admin` -> `/tutor` route).
- **Service Registration**: Update `Infrastructure\DependencyInjection.cs` to use the real AI service.
- **Configuration**: Add AI service settings to `appsettings.json`.
