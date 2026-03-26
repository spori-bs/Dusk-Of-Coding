# Fix Keycloak Auth Pipeline — Context (State Holder)

> **Goal**: Fix the three cascading root causes (Cookie Policy, Issuer Mismatch, Cookie Cleanup) that produce the infinite redirect loop, WebSocket errors, and HTTP 431.
> **Source**: `phase12-2-fix-keycloak-auth-pipeline-master-prompt.md`
> **Analysis**: `blazor_keycloak_auth_analysis.md`

## 📈 Status Tracker

- [x] **Phase 1: Cookie Policy Fix (P0)**
  - [x] Add `app.UseCookiePolicy()` before `app.UseAuthentication()` in `Program.cs`
  - [x] Set `MinimumSameSitePolicy = SameSiteMode.Lax`
  - [x] Add `OnAppendCookie`/`OnDeleteCookie` to downgrade `SameSite.None` on non-HTTPS

- [x] **Phase 2: Issuer Mismatch Fix (P0)**
  - [x] Configure `ValidIssuers` array (localhost, Aspire SD, container fallback)
  - [x] Verify JWT `iss` claim matches a `ValidIssuer` entry
  - [x] Preserve Keycloak role mapping in `OnTokenValidated`

- [x] **Phase 3: Cookie Cleanup Fix (P1)**
  - [x] Fix `OnRemoteFailure` cookie deletion with `Path=/signin-oidc`
  - [x] Fix `OnRedirectToIdentityProvider` cleanup with path-aware deletion
  - [x] Verify no orphaned correlation/nonce cookies after failure

- [x] **Phase 4: Diagnostic Logging (P1)**
  - [x] Add diagnostic `OpenIdConnectEvents` with `[OIDC]` prefix logging
  - [x] Log both `OnAuthenticationFailed` (middleware) and `OnRemoteFailure` (IDP)
  - [x] Verify exact error message visible on intentional failure

- [x] **Phase 5: Cleanup & Verification**
  - [x] Remove 128KB Kestrel header limit once stable
  - [x] Remove `QUARKUS_HTTP_LIMITS_MAX_HEADER_SIZE` from AppHost Keycloak config
  - [ ] Full end-to-end login/logout cycle verified
  - [ ] Multi-cycle stress test (5+ login/logout) — no 431, no cookie buildup

## 📌 Dependencies
- Depends on Phase 12 (Keycloak Tuning & UI Refinement) — all items completed.
- Requires `blazor_keycloak_auth_analysis.md` for technical reference.
- Changes scoped to `DuskOfCoding.WebUi/Program.cs` and `DuskOfCoding.AppHost/AppHost.cs`.
