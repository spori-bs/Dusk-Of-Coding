# Blazor Server + Keycloak: Root-Cause Analysis

> Based on review of [Program.cs](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Program.cs), [App.razor](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Components/App.razor), [Routes.razor](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Components/Routes.razor), [MemoryCacheTicketStore.cs](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Services/MemoryCacheTicketStore.cs), [TokenProvider.cs](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Services/TokenProvider.cs)

---

## 1. Correlation Cookie Failure — The Direct Cause of the Redirect Loop

**Your configuration at [Program.cs:46-49](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Program.cs#L46-L49):**

```csharp
options.Cookie.SameSite = SameSiteMode.None;
options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;
```

**The problem:** You set `SameSite=None` on the **session cookie**. But the **correlation cookie** and **nonce cookie** are emitted by `OpenIdConnectHandler` internally, and they inherit their `SameSite` policy from the `CookiePolicy` middleware — which you **never register**.

Without `app.UseCookiePolicy()`, the `OpenIdConnectHandler` creates correlation cookies with the browser's default `SameSite=Lax`. Here is the failure sequence:

```
1. GET /login → 302 to Keycloak
   ↳ Set-Cookie: .AspNetCore.Correlation.xxx=N; Path=/signin-oidc; SameSite=Lax
   ↳ Set-Cookie: .AspNetCore.OpenIdConnect.Nonce.xxx=N; SameSite=Lax

2. Keycloak authenticates user → 302 back to /signin-oidc
   ↳ This is a cross-site POST (from Keycloak's domain)
   ↳ SameSite=Lax → browser DROPS the correlation cookie on cross-site POST

3. OpenIdConnectHandler.HandleRemoteAuthenticateAsync() searches for
   .AspNetCore.Correlation.{state} → NOT FOUND
   ↳ Throws: "Correlation failed."

4. OnRemoteFailure fires → redirects to "/" → user is unauthenticated
   → if any page triggers [Authorize] → Challenge → back to step 1 → ∞ loop
```

> [!CAUTION]
> If running on `http://` (not HTTPS), `SameSite=None` requires `Secure`, which is impossible on plain HTTP. Browsers silently reject `SameSite=None` cookies without the `Secure` flag. This is not a .NET behavior — it's the [RFC 6265bis](https://datatracker.ietf.org/doc/html/draft-ietf-httpbis-rfc6265bis-09#section-4.1.2.7) spec.

**The fix — add `CookiePolicyOptions` before `UseAuthentication()`:**

```csharp
// Add in Program.cs BEFORE app.UseAuthentication()
app.UseCookiePolicy(new CookiePolicyOptions
{
    // For local dev on http://, the browser requires SameSite=Lax or Unspecified.
    // SameSite=None + Secure=false is REJECTED by all modern browsers.
    MinimumSameSitePolicy = SameSiteMode.Lax,

    // This callback runs for EVERY cookie the middleware emits,
    // including the OIDC correlation and nonce cookies that you
    // cannot configure directly.
    OnAppendCookie = context =>
    {
        // In development (http://localhost), downgrade None → Unspecified
        // so the browser doesn't require the Secure flag.
        if (context.CookieOptions.SameSite == SameSiteMode.None
            && !context.Context.Request.IsHttps)
        {
            context.CookieOptions.SameSite = SameSiteMode.Unspecified;
        }
    },
    OnDeleteCookie = context =>
    {
        if (context.CookieOptions.SameSite == SameSiteMode.None
            && !context.Context.Request.IsHttps)
        {
            context.CookieOptions.SameSite = SameSiteMode.Unspecified;
        }
    }
});
```

> [!IMPORTANT]
> `MinimumSameSitePolicy = Lax` is the critical line. It overrides the `SameSite` value for ALL cookies emitted through the middleware pipeline — including the internal `.AspNetCore.Correlation.*` cookies that `OpenIdConnectHandler` creates and that you have **zero direct API access to configure**.

---

## 2. Circuit State — Why the WebSocket "Not in OPEN State" Error

**Your configuration at [App.razor:21](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Components/App.razor#L21):**

```html
<Routes @rendermode="InteractiveServer" />
```

This forces the `Routes` component (and the entire component tree including `AuthorizeRouteView`) into **Interactive Server** mode from the first render. Here is what happens:

```
1. Browser GETs /practice (an [Authorize] page)
2. Kestrel runs the OIDC middleware pipeline:
   - HttpContext.User is set from the session cookie
   - AuthenticationState is "Authenticated"
3. SSR prerender emits HTML for the authenticated view
4. Blazor.web.js bootstraps, establishes the SignalR WebSocket
5. The SignalR circuit starts — but now:
   - There is NO HttpContext in the circuit
   - The AuthenticationStateProvider reads from the CIRCUIT scope
   - The circuit's AuthenticationState is ANONYMOUS (no cookie available over WS)
6. AuthorizeRouteView sees "Anonymous" → triggers Challenge
7. Challenge initiates redirect OVER the already-open WebSocket
   ↳ SignalR: "WebSocket is not in the OPEN state" — because the
     redirect (NavigationManager.NavigateTo) tears down the circuit
```

**Why this happens technically:** The `ServerAuthenticationStateProvider` used in Blazor Server gets its `ClaimsPrincipal` from the circuit's hub connection, which is established AFTER the initial HTTP request. During SSR prerendering,  `HttpContext.User` is available. Once the circuit is up, `HttpContext` is `null`. If the token/session cookie was not properly persisted into the circuit's DI scope, the circuit sees an anonymous user.

Your [App.razor](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Components/App.razor) does try to bridge this gap via `PersistentComponentState` + [TokenProvider](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Services/TokenProvider.cs#3-7), but [TokenProvider](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Services/TokenProvider.cs#3-7) is a **scoped service holding an access token string** — it does not re-constitute the `ClaimsPrincipal` for the `AuthenticationStateProvider`. The `AuthorizeRouteView` never consults [TokenProvider](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Services/TokenProvider.cs#3-7); it consults `AuthenticationStateProvider`.

> [!IMPORTANT]
> The WebSocket error is a **symptom**, not a root cause. Once the correlation cookie fix is in place and the OIDC flow completes successfully, if the session cookie is properly set and contains a valid session ID pointing to your [MemoryCacheTicketStore](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Services/MemoryCacheTicketStore.cs#14-63), the middleware will hydrate `HttpContext.User` correctly during SSR, *and* the `ServerAuthenticationStateProvider` will inherit it for the circuit. The WebSocket error should resolve.

---

## 3. HTTP 431 Root Cause — Correlation Cookie Graveyard

**Your cleanup code at [Program.cs:82-98](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Program.cs#L82-L98):**

```csharp
OnRedirectToIdentityProvider = context =>
{
    var correlationCookies = context.Request.Cookies.Keys
        .Where(c => c.StartsWith(".AspNetCore.Correlation.") || c.StartsWith("OpenIdConnect.Nonce."))
        .OrderBy(c => c)  // ← ordering by NAME, not by creation time
        .ToList();

    if (correlationCookies.Count > 3)
    {
        foreach (var cookie in correlationCookies.Take(correlationCookies.Count - 2))
        {
            context.Response.Cookies.Delete(cookie);
        }
    }
    return Task.CompletedTask;
};
```

**The mechanism:** Each failed OIDC challenge creates a **new** correlation cookie and a **new** nonce cookie. The `OpenIdConnectHandler` only deletes these cookies on a **successful** callback. If the callback keeps failing (due to the `SameSite` issue above), the cookies accumulate:

```
Request headers after 10 failed attempts:
  Cookie: .AspNetCore.Correlation.OpenIdConnect.aaa=...
  Cookie: .AspNetCore.Correlation.OpenIdConnect.bbb=...
  Cookie: OpenIdConnect.Nonce.xxx=...  (contains the full id_token nonce)
  Cookie: OpenIdConnect.Nonce.yyy=...
  ... × 10
```

Each nonce cookie value is ~600 bytes (it's base64-encoded). After ~15 failed cycles, the aggregate `Cookie` header exceeds Kestrel's default `MaxRequestHeadersTotalSize` (32KB) → **HTTP 431**.

**Your fix (bumping to 128KB at [Program.cs:14](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Program.cs#L14)) and the ticket cache ([MemoryCacheTicketStore](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Services/MemoryCacheTicketStore.cs#14-63)) correctly reduce the session cookie size**, but they don't address **why** correlation cookies accumulate. The why is the `SameSite` issue from §1.

**To properly clean up on failure**, your `OnRemoteFailure` handler at [Program.cs:127-140](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Program.cs#L127-L140) does delete stale cookies, but there's a subtle bug — `context.Response.Cookies.Delete(cookie)` emits a `Set-Cookie` with `expires=epoch`, but **without matching the `Path` and `SameSite` attributes** of the original cookie. If the original cookie was set with `Path=/signin-oidc`, a `Delete()` without that path targets `Path=/` — the browser keeps the original.

**Robust cleanup:**

```csharp
OnRemoteFailure = context =>
{
    foreach (var cookie in context.Request.Cookies.Keys)
    {
        if (cookie.StartsWith(".AspNetCore.Correlation.") || cookie.StartsWith("OpenIdConnect.Nonce."))
        {
            // Must match the Path the middleware originally set
            context.Response.Cookies.Delete(cookie, new CookieOptions
            {
                Path = "/signin-oidc",
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Unspecified
            });
            // Also delete at root path in case of mismatch
            context.Response.Cookies.Delete(cookie, new CookieOptions
            {
                Path = "/",
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Unspecified
            });
        }
    }
    context.Response.Redirect("/");
    context.HandleResponse();
    return Task.CompletedTask;
}
```

---

## 4. Diagnostic OpenIdConnectEvents

Replace your current `options.Events` block with this to get **exact protocol-level diagnostics**:

```csharp
options.Events = new OpenIdConnectEvents
{
    OnRedirectToIdentityProvider = context =>
    {
        Console.WriteLine($"[OIDC] ──── REDIRECT TO IDP ────");
        Console.WriteLine($"[OIDC] ProtocolMessage.IssuerAddress: {context.ProtocolMessage.IssuerAddress}");
        Console.WriteLine($"[OIDC] ProtocolMessage.RedirectUri:   {context.ProtocolMessage.RedirectUri}");
        Console.WriteLine($"[OIDC] ProtocolMessage.State:         {context.ProtocolMessage.State}");
        Console.WriteLine($"[OIDC] Request cookies ({context.Request.Cookies.Count}):");
        foreach (var c in context.Request.Cookies)
        {
            if (c.Key.StartsWith(".AspNetCore.") || c.Key.StartsWith("OpenIdConnect."))
                Console.WriteLine($"[OIDC]   {c.Key} = {c.Value[..Math.Min(40, c.Value.Length)]}...");
        }
        return Task.CompletedTask;
    },

    OnMessageReceived = context =>
    {
        Console.WriteLine($"[OIDC] ──── MESSAGE RECEIVED (callback) ────");
        Console.WriteLine($"[OIDC] ProtocolMessage.Code:  {context.ProtocolMessage.Code?[..Math.Min(20, context.ProtocolMessage.Code?.Length ?? 0)]}...");
        Console.WriteLine($"[OIDC] ProtocolMessage.Error: {context.ProtocolMessage.Error}");
        Console.WriteLine($"[OIDC] ProtocolMessage.ErrorDescription: {context.ProtocolMessage.ErrorDescription}");
        Console.WriteLine($"[OIDC] ProtocolMessage.State: {context.ProtocolMessage.State}");
        Console.WriteLine($"[OIDC] Request cookies ({context.Request.Cookies.Count}):");
        foreach (var c in context.Request.Cookies)
        {
            if (c.Key.StartsWith(".AspNetCore.") || c.Key.StartsWith("OpenIdConnect."))
                Console.WriteLine($"[OIDC]   {c.Key} = {c.Value[..Math.Min(40, c.Value.Length)]}...");
        }
        return Task.CompletedTask;
    },

    OnTokenValidated = context =>
    {
        Console.WriteLine($"[OIDC] ──── TOKEN VALIDATED ────");
        var identity = context.Principal?.Identity as ClaimsIdentity;
        if (identity != null)
        {
            Console.WriteLine($"[OIDC] IsAuthenticated: {identity.IsAuthenticated}");
            Console.WriteLine($"[OIDC] Name:            {identity.Name}");
            Console.WriteLine($"[OIDC] Claims ({identity.Claims.Count()}):");
            foreach (var claim in identity.Claims)
                Console.WriteLine($"[OIDC]   {claim.Type} = {claim.Value[..Math.Min(80, claim.Value.Length)]}");
        }

        Console.WriteLine($"[OIDC] Properties:");
        foreach (var prop in context.Properties?.Items ?? [])
            Console.WriteLine($"[OIDC]   {prop.Key} = {prop.Value?[..Math.Min(80, prop.Value?.Length ?? 0)]}");

        // --- Keycloak role mapping (keep your existing logic) ---
        if (identity != null)
        {
            var realmAccessClaim = identity.FindFirst("realm_access");
            if (realmAccessClaim != null)
            {
                using var doc = JsonDocument.Parse(realmAccessClaim.Value);
                if (doc.RootElement.TryGetProperty("roles", out var roles))
                {
                    foreach (var role in roles.EnumerateArray())
                    {
                        var rv = role.GetString();
                        if (!string.IsNullOrEmpty(rv))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, rv));
                            Console.WriteLine($"[OIDC]   Mapped Keycloak role → {rv}");
                        }
                    }
                }
            }
        }
        return Task.CompletedTask;
    },

    OnAuthenticationFailed = context =>
    {
        Console.WriteLine($"[OIDC] ──── AUTHENTICATION FAILED ────");
        Console.WriteLine($"[OIDC] Exception Type:    {context.Exception.GetType().Name}");
        Console.WriteLine($"[OIDC] Exception Message: {context.Exception.Message}");
        if (context.Exception.InnerException != null)
            Console.WriteLine($"[OIDC] Inner:             {context.Exception.InnerException.Message}");
        return Task.CompletedTask;
    },

    OnRemoteFailure = context =>
    {
        Console.WriteLine($"[OIDC] ──── REMOTE FAILURE ────");
        Console.WriteLine($"[OIDC] Failure.Message:   {context.Failure?.Message}");
        if (context.Failure?.InnerException != null)
            Console.WriteLine($"[OIDC] Failure.Inner:     {context.Failure.InnerException.Message}");

        // Check for protocol-level errors (invalid_grant, etc.)
        var error = context.HttpContext.Request.Query["error"].FirstOrDefault();
        var errorDesc = context.HttpContext.Request.Query["error_description"].FirstOrDefault();
        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine($"[OIDC] Protocol Error:       {error}");
            Console.WriteLine($"[OIDC] Protocol Description: {errorDesc}");
        }

        // CRITICAL: Clean up cookies to prevent 431 buildup
        foreach (var cookie in context.Request.Cookies.Keys)
        {
            if (cookie.StartsWith(".AspNetCore.Correlation.") || cookie.StartsWith("OpenIdConnect.Nonce."))
            {
                context.Response.Cookies.Delete(cookie, new CookieOptions
                {
                    Path = "/signin-oidc",
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Unspecified
                });
                context.Response.Cookies.Delete(cookie, new CookieOptions
                {
                    Path = "/",
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Unspecified
                });
                Console.WriteLine($"[OIDC]   Deleted stale cookie: {cookie}");
            }
        }

        context.Response.Redirect("/");
        context.HandleResponse();
        return Task.CompletedTask;
    }
};
```

> [!TIP]
> Run with this diagnostic code, trigger a login, and look for the `[OIDC]` prefixed lines. The **first thing you should see** if the correlation issue is present is: `AUTHENTICATION FAILED` with `Exception Message: Correlation failed.` — NOT `REMOTE FAILURE`. The `OnAuthenticationFailed` fires for middleware-internal failures (correlation, nonce validation). `OnRemoteFailure` fires when the remote IDP returns an error in the callback (like `error=invalid_grant`).

---

## 5. Why AuthenticationState Stays "Anonymous" — The Full Chain

The transition `Anonymous → Authenticated` requires this **exact** sequence to complete without interruption:

```
GET /login
  └→ ChallengeResult
       └→ OpenIdConnectHandler.HandleChallengeAsync()
            └→ 302 to Keycloak (sets correlation + nonce cookies)

POST /signin-oidc  (Keycloak callback)
  └→ OpenIdConnectHandler.HandleRemoteAuthenticateAsync()
       ├→ Validate correlation cookie  ← FAILS HERE (SameSite issue)
       ├→ Exchange code for tokens
       ├→ ValidateToken (fires OnTokenValidated)
       ├→ Create ClaimsPrincipal
       └→ SignIn via CookieAuthenticationHandler
            └→ Serialize ticket → MemoryCacheTicketStore.StoreAsync()
            └→ Set-Cookie: .AspNetCore.Cookies=<session-id>; HttpOnly
            └→ 302 to RedirectUri

GET /practice  (RedirectUri)
  └→ CookieAuthenticationHandler.AuthenticateAsync()
       └→ Read .AspNetCore.Cookies → MemoryCacheTicketStore.RetrieveAsync()
       └→ HttpContext.User = ClaimsPrincipal (Authenticated)
  └→ SSR renders the component tree
  └→ ServerAuthenticationStateProvider sees HttpContext.User = Authenticated ✓
```

If step 2 fails at "Validate correlation cookie", the entire chain short-circuits. No `ClaimsPrincipal` is created, no session cookie is set, `HttpContext.User` stays `Anonymous`, and any `[Authorize]` attribute or `AuthorizeRouteView` triggers a new challenge — creating the loop.

---

## Summary of Required Changes

| Priority | Change | File | Why |
|----------|--------|------|-----|
| **P0** | Add `app.UseCookiePolicy(...)` before `app.UseAuthentication()` | [Program.cs:198](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Program.cs#L198) | Fixes the correlation cookie being dropped by browsers |
| **P0** | Fix cookie `Delete()` calls to include `Path` + `SameSite` | [Program.cs:127-140](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Program.cs#L127-L140) | Ensures stale cookies are actually removed |
| **P1** | Add diagnostic `OpenIdConnectEvents` | [Program.cs:80](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Program.cs#L80) | Reveals exact failure point in OIDC flow |
| **P2** | Remove the 128KB header limit bump once flow is stable | [Program.cs:14](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Program.cs#L14) | Shouldn't be needed once cookies stop accumulating |

---

## 6. The Dual-Network Issuer Mismatch (Podman/Aspire)

> [!CAUTION]
> Even after fixing the correlation cookie issue, the OIDC flow will likely fail at **token validation** with an issuer mismatch. This is a separate, deeper problem inherent to how Aspire wires Keycloak in a container.

### What Aspire Actually Does

I read the [actual source of `AddKeycloakOpenIdConnect`](https://github.com/dotnet/aspire/blob/main/src/Components/Aspire.Keycloak.Authentication/AspireKeycloakExtensions.cs). Here is the critical code:

```csharp
// From AspireKeycloakExtensions.cs (actual source)
builder.Services
    .AddOptions<OpenIdConnectOptions>(authenticationScheme)
    .Configure<IConfiguration, IHttpClientFactory, IHostEnvironment>(
        (options, configuration, httpClientFactory, hostEnvironment) =>
        {
            options.Backchannel = httpClientFactory.CreateClient(KeycloakBackchannel);
            options.Authority = GetAuthorityUri(serviceName, realm);
            configureOptions?.Invoke(options);  // ← YOUR callback runs AFTER
        });

private static string GetAuthorityUri(string serviceName, string realm)
{
    return $"https+http://{serviceName}/realms/{realm}";
    // → "https+http://keycloak/realms/DuskOfCoding"
}
```

The `https+http://` prefix is an Aspire service discovery URI scheme. Aspire's `ServiceDiscoveryHttpClientFactory` resolves it to the container's actual address (e.g., `http://10.89.0.5:8080/realms/DuskOfCoding` on the Podman network).

### The Two Networks

```
┌──────────────────────────────────────────────────────────┐
│  Browser (User Agent)                                    │
│  Sees: http://localhost:8080/realms/DuskOfCoding         │
│  ↳ This is what the user types / gets redirected to      │
│  ↳ Keycloak stamps this URL as the "iss" claim in JWT    │
└───────────────────────┬──────────────────────────────────┘
                        │ 302 (authorization code)
                        ▼
┌──────────────────────────────────────────────────────────┐
│  WebUI Kestrel (inside Aspire orchestrator)              │
│  Authority = "https+http://keycloak/realms/DuskOfCoding" │
│  Resolved by SD → http://10.89.0.5:8080/realms/...      │
│                                                          │
│  Token validation checks:                                │
│    JWT iss:  "http://localhost:8080/realms/DuskOfCoding"  │
│    Expected: "http://10.89.0.5:8080/realms/DuskOfCoding" │
│    → MISMATCH → SecurityTokenInvalidIssuerException      │
└──────────────────────────────────────────────────────────┘
```

The `iss` claim in the JWT is determined by Keycloak's **frontend URL** (what the browser accessed). But `OpenIdConnectHandler.ValidateToken()` compares it against the **Authority** it used to download the OIDC metadata — which is the internal container URL.

### The Fix

Your `configureOptions` callback runs **after** Aspire sets `Authority`, so you can override or supplement the validation. There are two viable approaches:

#### Option A: `ValidIssuers` Array (Recommended)

```csharp
// In your AddKeycloakOpenIdConnect configureOptions callback:
options.TokenValidationParameters = new TokenValidationParameters
{
    RoleClaimType = ClaimTypes.Role,
    ValidateIssuer = true,  // Keep validation ON
    ValidIssuers = new[]
    {
        // The external URL the browser (and Keycloak's iss claim) uses:
        "http://localhost:8080/realms/DuskOfCoding",
        // The internal URL Aspire resolves via service discovery:
        "https+http://keycloak/realms/DuskOfCoding",
        // In case Aspire resolves it differently:
        "http://keycloak:8080/realms/DuskOfCoding",
    }
};
```

#### Option B: Split Authority vs MetadataAddress

```csharp
options.Authority = "http://localhost:8080/realms/DuskOfCoding";  // external, matches iss
options.MetadataAddress = "https+http://keycloak/realms/DuskOfCoding/.well-known/openid-configuration";  // internal, for backchannel
options.RequireHttpsMetadata = false;
```

**Why this works:** `Authority` is used for two things: (1) building the `MetadataAddress` if not explicitly set, and (2) issuer validation. By setting `Authority` to the external URL, issuer validation passes. By explicitly setting `MetadataAddress` to the internal service-discovery URL, the backchannel metadata fetch still goes through the Podman network — no external roundtrip.

> [!IMPORTANT]
> Option B is cleaner for production because it keeps `ValidateIssuer = true` without maintaining a list. Option A is safer during dev when you're not sure exactly which URL Keycloak stamps as `iss`.

### Aspire Service Discovery in AppHost

Your current [AppHost.cs:11](file:///d:/Repos/PracticePlatform/DuskOfCoding.AppHost/AppHost.cs#L11) uses:

```csharp
var keycloak = builder.AddKeycloak("keycloak", 8080)
```

The port `8080` is exposed to the host, so the browser reaches Keycloak at `http://localhost:8080`. But the Aspire-internal references (`.WithReference(keycloak)`) resolve `"keycloak"` to the container's internal endpoint. If you need to make the external URL available to the WebUI as configuration, you can use:

```csharp
// In AppHost.cs — expose external endpoint as a connection string parameter
var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume()
    .WithRealmImport("../KeycloakConfig");

builder.AddProject<Projects.DuskOfCoding_WebUi>("webui")
    .WithReference(keycloak)
    .WithEnvironment("KEYCLOAK_EXTERNAL_URL", keycloak.GetEndpoint("http"));
```

Then in `WebUI/Program.cs`:

```csharp
var keycloakExternal = builder.Configuration["KEYCLOAK_EXTERNAL_URL"];
// Use this for Authority
```

---

## Updated Priority Table

| Priority | Change | Why |
|----------|--------|-----|
| **P0** | Add `app.UseCookiePolicy(...)` with `SameSite` downgrade | Fixes correlation cookie rejection |
| **P0** | Set `ValidIssuers` or split `Authority`/`MetadataAddress` | Fixes issuer mismatch after correlation is fixed |
| **P1** | Fix cookie `Delete()` to include `Path` | Ensures stale cookies are removed |
| **P1** | Add diagnostic `OpenIdConnectEvents` | Reveals exact failure point |
| **P2** | Remove 128KB header limit once stable | Symptom mitigation no longer needed |
