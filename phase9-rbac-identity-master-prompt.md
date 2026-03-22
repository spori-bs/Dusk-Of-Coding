# RBAC & Identity UX - Master Prompt

## Objective
Introduce Role-Based Access Control with `admin` and `student` personas, refine the Landing Page and Navigation to respect authentication state, and build an Admin Dashboard for aggregate student analytics.

## Context
Currently all authenticated users see the same navigation links ("Practice" and "Manage Tasks"). There is no role distinction — any logged-in user can manage tasks, which should be an admin-only privilege. Additionally, the Landing Page Hero section and LeadFormComponent expose action buttons that silently fail for unauthenticated users.

### Current Architecture
- **Authentication**: Keycloak OIDC (WebUi) + JWT Bearer (WebApi).
- **User Identification**: `ClaimTypes.NameIdentifier` → `Guid` from Keycloak `sub` claim.
- **NavMenu.razor**: Static links to Home, Practice, Manage Tasks, and `<LoginDisplay>`.
- **HeroComponent.razor**: "Start Practicing" links to `/login?returnUrl=/practice`, "See How It Works" scrolls to `#demo`.
- **LeadFormComponent.razor**: Multi-step registration wizard → redirects to Keycloak `/register`.

## Implementation Roadmap

### Phase 1: Keycloak Role Configuration
1. **Update `realm.json`**:
   - Add realm-level roles: `admin`, `student`.
   - Assign `admin` role to the default test user.
   - Add a protocol mapper of type `User Realm Role` to the `webui` client scope so that realm roles appear in the JWT `realm_access.roles` claim.
2. **Backend Role Mapping (WebApi)**:
   - Keycloak sends roles in `realm_access.roles` (a nested JSON array). Configure `JwtBearerOptions.TokenValidationParameters.RoleClaimType` or add a custom claims transformation so that `ClaimsPrincipal.IsInRole("admin")` works correctly.
   - Test with: `[Authorize(Roles = "admin")]` on task management endpoints (POST, PUT, DELETE `/tasks`).
   - Keep GET `/tasks` and GET `/tasks/{id}` open to all authenticated users (students need to see the task list for practice).
3. **Frontend Role Mapping (WebUi)**:
   - Add a claims transformation or configure `OpenIdConnectOptions.ClaimActions` to map `realm_access.roles` into flat role claims that `<AuthorizeView Roles="...">` can evaluate.

### Phase 2: Navigation & Landing Page Refactor
1. **NavMenu.razor**:
   - Wrap "Manage Tasks" link in `<AuthorizeView Roles="admin">`.
   - "Practice" remains visible to all authenticated users.
   - Add a dedicated "Login" nav link visible only to unauthenticated users (via `<AuthorizeView>` `NotAuthorized` template).
2. **HeroComponent.razor**:
   - Wrap CTAs in `<AuthorizeView>`:
     - **Authenticated**: Show "Start Practicing" → `/practice`.
     - **Not Authenticated**: Show "Login" (Primary) → `/login` and "Register" (Secondary) → `/register`.
3. **LeadFormComponent.razor**:
   - Only render for unauthenticated visitors. Hide entirely for logged-in users (they've already registered).
4. **Anti-Flicker**: Use the `<Authorizing>` template to show a subtle skeleton/spinner while the auth state resolves, preventing UI jumps.

### Phase 3: Admin Dashboard
1. **New Page: `/admin`** (`Components/Pages/AdminDashboard.razor`):
   - Protected with `@attribute [Authorize(Roles = "admin")]`.
   - Displays aggregate analytics pulled from WebApi:
     - Total registered students (distinct `UserId` in Submissions).
     - Total submissions across all tasks.
     - Per-task breakdown: success rate, average attempts, most common failure reasons (from `FeedbackRecord` summaries).
     - A simple table or card grid — premium glassmorphism styling.
2. **WebApi Endpoints**:
   - `GET /admin/stats` → `[Authorize(Roles = "admin")]` — returns global platform analytics.
   - Reuse/extend the existing `/tasks/{id}/stats` endpoint data.
3. **NavMenu**: Add "Dashboard" link visible only to `admin` role.

## Technical Notes
- **Keycloak Realm Roles vs Client Roles**: Use **realm roles** for simplicity. Client roles add unnecessary complexity for this use case.
- **Default Role Assignment**: Consider configuring Keycloak to auto-assign `student` as a default role for all new registrations.
- **Blazor `AuthorizeView` and SSR**: During static SSR, `AuthorizeView` may not have auth state. Since all protected pages use `@rendermode InteractiveServer`, this should resolve after the circuit connects. The `<Authorizing>` template handles the brief loading state.
