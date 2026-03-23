# Error Handling & Resilience - Master Prompt

## Objective
Replace the default Blazor error bar with a premium, branded global error boundary and implement graceful HTTP error handling across the entire application.

## Context
The application currently uses Blazor's default error UI — a plain reload bar at the bottom of the viewport. This is jarring for a platform that invests heavily in a premium glassmorphism dark aesthetic. Additionally, when API calls return 401 (Unauthorized) or 403 (Forbidden), the errors propagate as unhandled exceptions visible in the browser console, degrading the user experience.

### Known Issues
- **Default Blazor Error Bar**: Unbranded and breaks the visual identity.
- **Console 401/403 Noise**: Unauthenticated API calls (e.g., during SSR pre-rendering or expired tokens) throw `HttpRequestException` with no user-facing recovery path.
- **Cookie Bloat (localhost)**: OIDC tokens on `localhost` can trigger Keycloak's "431 Request Header Fields Too Large". Already mitigated with `ChunkingCookieManager` and `QUARKUS_HTTP_LIMITS_MAX_HEADER_SIZE=32k` — maintain these fixes.

## Implementation Roadmap

### Phase 1: Global Error Boundary
1. **Custom `<ErrorBoundary>`**:
   - Wrap the main content area in `MainLayout.razor` with a `<ErrorBoundary>` component.
   - Provide a custom `<ErrorContent>` template that renders a branded error panel.
2. **`CustomErrorContent.razor`** (new component):
   - Full-viewport overlay matching the Dusk of Coding aesthetic (glassmorphism panel, centered).
   - Display a friendly error message: "Something went wrong."
   - Include a prominent "Try Again" button that calls `ErrorBoundary.Recover()`.
   - Optionally show a collapsible technical details section (dev mode only).
3. **Design Tokens**:
   - Background: `rgba(15, 23, 42, 0.95)` with `backdrop-filter: blur(16px)`.
   - Error accent: Subtle red/orange gradient border.
   - Button: Use existing `.dusk-btn-outline` style.

### Phase 2: Graceful 401/403 Handling
1. **`AuthDelegatingHandler`** (new class in `WebUi/Services/`):
   - A custom `DelegatingHandler` registered in the `ApiClient` `HttpClient` pipeline.
   - Intercept 401 responses → redirect to `/login?returnUrl=<current-page>`.
   - Intercept 403 responses → redirect to a new `/access-denied` page.
2. **Access Denied Page** (`Components/Pages/AccessDenied.razor`):
   - Branded page with a lock icon, clear message ("You don't have permission to view this page"), and a "Go Home" button.
   - Does NOT require `[Authorize]`.
3. **ApiClient Hardening**:
   - Wrap `GetFromJsonAsync` / `PostAsJsonAsync` calls with try-catch for `HttpRequestException`.
   - Return sensible fallback values (empty list, null) instead of letting exceptions bubble up to the ErrorBoundary for routine 401s.

### Phase 3: Validation & Polish
1. Verify that expired tokens during an interactive session gracefully redirect to login.
2. Verify that the ErrorBoundary correctly catches unhandled exceptions from SignalR callbacks.
3. Ensure no raw exception stack traces appear in the browser console in production builds.

## Technical Notes
- The `OnAfterRenderAsync` pattern is used for API calls (not `OnInitializedAsync`) to avoid SSR pre-rendering 401s. Maintain this pattern.
- The `TokenProvider` is scoped and populated from `HttpContext` — it will be `null` during static SSR. The `AuthDelegatingHandler` should handle this gracefully.
