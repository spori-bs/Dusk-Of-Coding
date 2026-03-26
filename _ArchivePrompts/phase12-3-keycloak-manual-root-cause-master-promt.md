# Master Prompt: Phase 12.3 — Keycloak Manual Root Cause & Security Audit

## Context
This phase addressed a critical "Death Loop" in the .NET 10 Aspire architecture where students were stuck in an infinite redirect cycle between the `WebUi` and `Keycloak`, alongside several high-priority security vulnerabilities identified during a principal engineer audit.

## Core Issues & Root Causes

### 1. The Infinite Redirect "Death Loop"
- **Internal vs Public Identity**: `WebApi` was only configured to trust tokens issued by the internal container URL (`keycloak:8080`). However, tokens presented by the browser (via the `WebUi`) contained the public issuer (`localhost:8080`).
- **The Loop**: `WebApi` returned `401 Unauthorized` (Issuer Mismatch) -> `ApiClient` forced a hard redirect to `/login` -> `Keycloak` saw a valid SSO session and redirected back -> `WebApi` rejected it again.
- **Fix**: Synchronized `ValidIssuers` in `WebApi` to match the multi-origin trust list in `WebUi`.

### 2. The Blazor "Plaintext" Token Leak
- **Vulnerability**: The `access_token` was being serialized into the HTML body via `PersistentComponentState` (SSR to Interactive bridge). This exposed the full JWT to the client DOM, vulnerable to XSS.
- **Fix**: Removed `ApplicationState.PersistAsJson` for tokens. Refactored the scoped `TokenProvider` to fetch the token dynamically from the server-side `AuthenticationStateProvider` using the `access_token` claim.

### 3. Identity & Role Mapping Mismatch
- **Vulnerability**: `WebUi` was manually parsing `realm_access` JSON to extract roles, while `WebApi` expected a standard `"roles"` claim.
- **Fix**: Standardized both services on `RoleClaimType = "roles"`. Leveraged Keycloak Protocol Mappers to surface realm roles directly into a standard array claim.

### 4. Client Confidentiality
- **Vulnerability**: `webui` was set as a `publicClient: true`.
- **Fix**: Updated `realm.json` to `publicClient: false` and implemented `ClientSecret` authentication in the OIDC backchannel.

### 5. The "UntrustedRoot" SSL Dilemma
- **Challenge**: Disabling `DangerousAcceptAnyServerCertificateValidator` broke local dev environments because Podman containers don't share the host's Windows Dev Cert trust chain.
- **Fix**: Implemented a conditional bypass: `if (builder.Environment.IsDevelopment())`. This preserves production security while maintaining developer velocity.

## Technical Remediation Summary

### Keycloak Config (realm.json)
- Set `publicClient: false` for `webui`.
- Added `"secret": "secret"`.

### WebUi (Program.cs)
- `RoleClaimType = "roles"`.
- Added `ClientSecret = "secret"`.
- Removed `OnTokenValidated` JSON role-parsing hack.
- Added `identity.AddClaim(new Claim("access_token", ...))` to preserve token for internal use without DOM exposure.

### WebApi (Program.cs)
- Added `ValidIssuers` array to support both `localhost` and `keycloak` container DNS.
- Implemented `if (IsDevelopment)` SSL bypass.

### Blazor Components
- **Routes.razor**: Deleted `PersistToken` and `ApplicationState` registration.
- **App.razor**: Removed `GetTokenAsync` SSR seeding.
- **ApiClient.cs**: Refactored to `await EnsureAuthHeaderAsync()`.

## Verification Status
- [x] Build Clean (0 Errors).
- [x] No `access_token` found in Page Source (F12).
- [x] Student login to `/practice` succeeds without loops.
- [x] `[Authorize(Roles="admin")]` correctly gates `AdminDashboard`.
