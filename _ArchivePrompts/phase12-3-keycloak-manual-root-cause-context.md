# Phase 12.3 Context: Keycloak Root Cause & Security Hardening

## Architectural Overview
This phase was triggered by a recurring "UntrustedRoot" error and infinite redirect loops encountered in the local development environment (Podman/Aspire). The project uses a multi-service architecture (WebUi, WebApi, Keycloak) where secure token propagation and trust chains are critical.

## Security Audit Findings (Addressed)
1. **Token Leakage**: OIDC `access_token` was leaking into the public HTML DOM via Blazor's `PersistentComponentState`. High risk of XSS token theft.
2. **Standardized RBAC**: Inconsistent role claim types (`realm_access` vs `roles`) led to authorization bypasses or loops.
3. **Public Client Risk**: The `webui` client was incorrectly configured as a public client instead of a confidential one requiring a secret.
4. **Development Hacks**: Excessive use of `DangerousAcceptAnyServerCertificateValidator` masked deeper infrastructure misconfigurations.

## Implementation Details

### Secure Token Handling
Tokens are now handled exclusively on the server side. During the OIDC login flow, the `access_token` is stored as a temporary claim within the `ClaimsIdentity`. The `TokenProvider` (scoped service) retrieves this claim via `AuthenticationStateProvider`, ensuring the token never touches the browser's JavaScript space or rendered HTML.

### Cross-Network Trust
Local development uses a conditional SSL bypass for the internal Keycloak backchannel. This is necessary because Aspire's dev-cert injection does not always propagate correctly to Podman container volumes on Windows hosts.

### Issuer Synchronization
The `ValidIssuers` property in `WebApi` now explicitly trusts:
- `http://localhost:8080/realms/DuskOfCoding` (Public/Browser origin)
- `https+http://keycloak/realms/DuskOfCoding` (Aspire Proxy origin)
- `http://keycloak:8080/realms/DuskOfCoding` (Internal Container origin)

This prevents the `401 Unauthorized` responses that previously triggered the infinite redirect loop.

## Key Files Modified
- `DuskOfCoding.WebUi/Program.cs`
- `DuskOfCoding.WebApi/Program.cs`
- `DuskOfCoding.WebUi/Services/TokenProvider.cs`
- `DuskOfCoding.WebUi/Services/ApiClient.cs`
- `DuskOfCoding.WebUi/Components/Routes.razor`
- `DuskOfCoding.WebUi/Components/App.razor`
- `KeycloakConfig/realm.json`
