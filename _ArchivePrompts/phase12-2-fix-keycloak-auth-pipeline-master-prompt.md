# Fix Keycloak Auth Pipeline — Master Prompt

## Objective
Act as a Senior Identity Architect with deep expertise in OpenID Connect, ASP.NET Core Authentication Middleware, and containerized identity infrastructure. Your goal is to fix the broken OIDC authentication pipeline between the Blazor Server WebUI and Keycloak running in a Podman container, orchestrated by .NET Aspire.

## Problem Statement
The current integration suffers from three cascading failures that produce an infinite redirect loop, WebSocket "not in OPEN state" errors, and intermittent HTTP 431 responses. All three share a single chain of causality:

1. **Correlation Cookie Rejection** → The browser drops `.AspNetCore.Correlation.*` cookies because no `CookiePolicyOptions` middleware exists to govern their `SameSite`/`Secure` attributes in a local HTTP dev environment.
2. **Issuer Mismatch** → Even if the correlation cookie were present, token validation would fail because Aspire's `AddKeycloakOpenIdConnect` sets `Authority` to an internal service-discovery URI (`https+http://keycloak/realms/DuskOfCoding`), while Keycloak stamps the JWT `iss` claim with the browser-facing `http://localhost:8080/realms/DuskOfCoding`.
3. **Correlation Cookie Accumulation** → Each failed OIDC handshake leaves orphaned correlation and nonce cookies. Without path-aware deletion, they build up until request headers exceed Kestrel's limit → HTTP 431.

*Root-cause analysis documented in `blazor_keycloak_auth_analysis.md`.*

## Constraints
- **Do NOT** suggest "clearing cookies manually" or "increasing Kestrel header limits" as solutions. Address the protocol-level root causes.
- **Do NOT** set `ValidateIssuer = false`. Use a `ValidIssuers` array or split `Authority`/`MetadataAddress`.
- **Do NOT** remove the existing `MemoryCacheTicketStore`. It correctly solves post-authentication cookie bloat from `SaveTokens = true`.
- **Preserve** existing Keycloak role mapping (`realm_access.roles` → `ClaimTypes.Role`) in `OnTokenValidated`.
- **Preserve** existing `ReturnUrl` handling on `/login`, `/logout`, `/register` endpoints.

---

## Implementation Roadmap

### Phase 1: Fix the Cookie Policy (P0)

**File:** `DuskOfCoding.WebUi/Program.cs`

1. Register `app.UseCookiePolicy(...)` **before** `app.UseAuthentication()`.
2. Set `MinimumSameSitePolicy = SameSiteMode.Lax`.
3. Add `OnAppendCookie` and `OnDeleteCookie` callbacks that downgrade `SameSite.None` → `SameSite.Unspecified` when `Request.IsHttps == false`.
4. This ensures the `OpenIdConnectHandler`'s internal correlation and nonce cookies are not rejected by the browser on `http://localhost`.

### Phase 2: Fix the Issuer Mismatch (P0)

**File:** `DuskOfCoding.WebUi/Program.cs`

Aspire's `AddKeycloakOpenIdConnect` source code (verified from [GitHub](https://github.com/dotnet/aspire/blob/main/src/Components/Aspire.Keycloak.Authentication/AspireKeycloakExtensions.cs)) sets:
```
options.Authority = $"https+http://{serviceName}/realms/{realm}"
```
Then invokes `configureOptions`, meaning **our callback can override it**.

**Choose one approach:**

- **Option A (Dev-friendly):** In `configureOptions`, set `TokenValidationParameters.ValidIssuers` to an array containing:
  - `http://localhost:8080/realms/DuskOfCoding` (browser/JWT issuer)
  - `https+http://keycloak/realms/DuskOfCoding` (Aspire SD URI)
  - `http://keycloak:8080/realms/DuskOfCoding` (fallback)

- **Option B (Production-ready):** Override `options.Authority` to the external URL (`http://localhost:8080/realms/DuskOfCoding`) and explicitly set `options.MetadataAddress` to the internal SD URI (`https+http://keycloak/realms/DuskOfCoding/.well-known/openid-configuration`). This separates issuer validation (external) from metadata fetching (internal).

### Phase 3: Fix Cookie Cleanup in OnRemoteFailure (P1)

**File:** `DuskOfCoding.WebUi/Program.cs`

The current `OnRemoteFailure` handler calls `context.Response.Cookies.Delete(cookie)` without matching the `Path` or `SameSite` of the original cookie. The correlation cookies are set with `Path=/signin-oidc` by the middleware.

1. Delete each stale cookie at **both** `Path=/signin-oidc` and `Path=/` to cover all cases.
2. Set `Secure = context.Request.IsHttps` and `SameSite = SameSiteMode.Unspecified`.
3. Apply the same path-aware deletion in `OnRedirectToIdentityProvider` cleanup.

### Phase 4: Add Diagnostic Logging (P1)

**File:** `DuskOfCoding.WebUi/Program.cs`

Replace the current `OpenIdConnectEvents` with a diagnostic version that logs to console with `[OIDC]` prefix:

- `OnRedirectToIdentityProvider` — log `ProtocolMessage.IssuerAddress`, `RedirectUri`, `State`, and all `.AspNetCore.*` cookies.
- `OnMessageReceived` — log `ProtocolMessage.Code`, `Error`, `ErrorDescription`, `State`.
- `OnTokenValidated` — log `IsAuthenticated`, `Name`, all claims, and `Properties.Items`.
- `OnAuthenticationFailed` — log `Exception.GetType().Name`, `Exception.Message`, inner exception.
- `OnRemoteFailure` — log `Failure.Message`, query string `error`/`error_description` (protocol errors like `invalid_grant`), and deleted cookies.

> **Important distinction:** `OnAuthenticationFailed` fires for middleware-internal failures (correlation, nonce). `OnRemoteFailure` fires for IDP-returned errors. Log both to differentiate.

### Phase 5: Cleanup & Verification

1. **Remove** the 128KB Kestrel header limit bump (`MaxRequestHeadersTotalSize = 131072`) once the flow is stable — it should no longer be needed.
2. **Remove** the `QUARKUS_HTTP_LIMITS_MAX_HEADER_SIZE` override from `AppHost.cs` Keycloak config for the same reason.
3. **Verify** the full flow: `/login` → Keycloak → `/signin-oidc` → redirect to `ReturnUrl` → `AuthorizeView` renders `<Authorized>` content.
4. **Verify** no correlation/nonce cookies remain after a successful login.
5. **Verify** `OnRemoteFailure` correctly logs and cleanly redirects if the IDP returns an error.

---

## Verification Checklist

- [ ] Login as `student` → lands on correct page, `AuthorizeView` shows authenticated content
- [ ] Login as `admin` → privilege badge visible, admin sections accessible
- [ ] Logout → session cookie cleared, `AuthorizeView` shows anonymous content
- [ ] No `.AspNetCore.Correlation.*` or `OpenIdConnect.Nonce.*` cookies persist post-login in browser DevTools → Application → Cookies
- [ ] WebSocket "not in OPEN state" error is gone
- [ ] No HTTP 431 after multiple login/logout cycles
- [ ] Console logs show `[OIDC] TOKEN VALIDATED` with correct claims on successful login
- [ ] Console logs show correct error details on intentional failure (e.g., expired code)

## Reference
- Root-cause analysis: `blazor_keycloak_auth_analysis.md`
- Aspire Keycloak source: [AspireKeycloakExtensions.cs](https://github.com/dotnet/aspire/blob/main/src/Components/Aspire.Keycloak.Authentication/AspireKeycloakExtensions.cs)
- Phase 12 original: `phase12-keycloak-tuning-master-promt.md`
