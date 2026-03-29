# Phase 14-2 Context: Authorization and Role Refactoring

## Current State
- `AppRoles.cs` created in `DuskOfCoding.Domain.Constants`.
- `Program.cs` in both WebUI and WebApi updated with `DefaultMapInboundClaims = false`.
- `Routes.razor` updated with `<RedirectToLogin />` for unauthenticated sessions.
- `HttpCodeExecutionEngine.cs` updated with default test bundle fallback.
- `SocraticTutorPrompt.cs` updated with stricter rules and Hungarian translation.
- `realm.json` updated with test accounts: `sysadmin`, `tutor`, `student`.

## Key Dependencies
- Microsoft.AspNetCore.Authorization
- Microsoft.Extensions.AI
- Keycloak (Dockerized)
