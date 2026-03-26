# Master Prompt: Diagnosing and Resolving HTTP 431 Header Size Errors

## Context
In modern distributed systems leveraging OpenID Connect (OIDC) with `SaveTokens = true`, a common failure mode is the **HTTP 431 Request Header Fields Too Large** error. This typically occurs during authentication redirects or when multiple middleware components (like ASP.NET DataProtection and Keycloak tokens) compete for browser cookie space.

## Deep Diagnostic Checklist (Verify Twice)
Before applying fixes, systematically verify these three tiers of the request chain:

### 1. The Client-Side (Browser)
- [x] **Cookie Count**: Check if `.AspNetCore.Correlation.` or `OpenIdConnect.Nonce.` cookies have multiplied. A failed auth loop can spawn dozens of these, each adding 100-200 bytes.
- [x] **Chunking Presence**: Are there multiple `.AspNetCore.CookiesC1`, `C2`, `C3`...? This confirms `SaveTokens = true` is pushing the payload over the ~4KB per-cookie limit.
- [x] **Large SSO Headers**: Check for `KEYCLOAK_IDENTITY` or `AUTH_SESSION_ID`. If the realm was recently reset, these might be "ghost" cookies from a previous crypto-key session.

### 2. The Infrastructure Layer (Orchestration)
- [x] **Keycloak/Quarkus Limit**: Verify `QUARKUS_HTTP_LIMITS_MAX_HEADER_SIZE` is set to at least `64k` or `128k`. Default is a lethal **8KB**.
- [x] **Reverse Proxy / Gateway**: If using YARP, NGINX, or Envoy, check their specific `MaxHeaderSize` settings. Even if the app allows 128KB, a proxy might clip it at 8KB.

### 3. The Application Host (Kestrel)
- [x] **Kestrel System Limit**: Ensure `options.Limits.MaxRequestHeadersTotalSize` is explicitly increased to `128KB`. The default is ~32KB.
- [x] **Form Limits**: If the 431 occurs during a POST (e.g., large form submission), check `options.Limits.MaxRequestBodySize`.

## Remediation Strategy

### 1. Infrastructure (AppHost)
Increase Keycloak's capacity to ingest bloated OIDC headers:
- **Env**: `QUARKUS_HTTP_LIMITS_MAX_HEADER_SIZE = 128k`

### 2. Service Code (WebApi/WebUi)
Patch Kestrel in `Program.cs` to tolerate the chunked token payload:
```csharp
builder.WebHost.ConfigureKestrel(options => {
    options.Limits.MaxRequestHeadersTotalSize = 131072; // 128KB
});
```

### 3. Manual Recovery
**Instruction to User**: If the loop persists, the browser state is likely poisoned. **Clear Localhost Cookies** to reset the correlation/nonce state.

## Final Verification
1. Open DevTools -> Network -> Click failed request.
2. Check `Request Headers` -> `Cookie`. 
3. Copy the entire cookie string into a character counter. If it's > 8KB, Quarkus (Keycloak) will fail. If it's > 32KB, Kestrel will fail.
