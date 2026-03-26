# Phase 12-2: Fix Keycloak Auth Pipeline — Walkthrough

## Changes Made

### [Program.cs](file:///d:/Repos/PracticePlatform/DuskOfCoding.WebUi/Program.cs)

| Phase | Change |
|-------|--------|
| **1** | Added `app.UseCookiePolicy()` **before** `app.UseAuthentication()` with `MinimumSameSitePolicy = Lax` and `OnAppendCookie`/`OnDeleteCookie` callbacks that downgrade `SameSite.None → Unspecified` on non-HTTPS |
| **2** | Added `ValidIssuers` array to `TokenValidationParameters` covering `localhost:8080`, `https+http://keycloak`, and `keycloak:8080` |
| **3** | Path-aware cookie deletion in both `OnRemoteFailure` and `OnRedirectToIdentityProvider` — deletes at `Path=/signin-oidc` and `Path=/` |
| **4** | Full `[OIDC]` diagnostic logging on all 5 event hooks (`OnRedirectToIdentityProvider`, `OnMessageReceived`, `OnTokenValidated`, `OnAuthenticationFailed`, `OnRemoteFailure`) |
| **5** | Removed 128KB Kestrel `MaxRequestHeadersTotalSize` workaround |

```diff:Program.cs
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using DuskOfCoding.WebUi.Components;
using DuskOfCoding.WebUi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestHeadersTotalSize = 131072; // 128KB
});

builder.AddServiceDefaults();

// ── Localization ───────────────────────────────────────────
builder.Services.AddLocalization();

// ── Dev certificate trust (Aspire service-to-service) ─────
if (builder.Environment.IsDevelopment())
{
    builder.Services.ConfigureHttpClientDefaults(http =>
    {
        http.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
    });
}

// ── Server-side Session Cache ──────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITicketStore, MemoryCacheTicketStore>();

// ── Authentication (Keycloak OIDC) ───────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
    ? CookieSecurePolicy.SameAsRequest 
    : CookieSecurePolicy.Always;
    options.CookieManager = new ChunkingCookieManager();
})
// Wire ITicketStore via PostConfigure so it runs AFTER AddCookie's own Configure,
// guaranteeing SessionStore is set without calling BuildServiceProvider().
.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<ITicketStore>((options, store) =>
    {
        options.SessionStore = store;
    });

builder.Services.AddAuthentication()  // no-op re-registration, just need the builder chain
.AddKeycloakOpenIdConnect(
    "keycloak",
    realm: "DuskOfCoding",
    configureOptions: options =>
    {
        options.ClientId = "webui";
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.RequireHttpsMetadata = false;
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            RoleClaimType = ClaimTypes.Role
        };

        // Fix for HTTP 431 Error (Cookie Bloat) + Keycloak role claim mapping
        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                // Clean up old correlation/nonce cookies but keep the last 2
                // so the current handshake is not broken
                var correlationCookies = context.Request.Cookies.Keys
                    .Where(c => c.StartsWith(".AspNetCore.Correlation.") || c.StartsWith("OpenIdConnect.Nonce."))
                    .OrderBy(c => c)
                    .ToList();

                if (correlationCookies.Count > 3)
                {
                    // Delete all except the last 2 (most recent)
                    foreach (var cookie in correlationCookies.Take(correlationCookies.Count - 2))
                    {
                        context.Response.Cookies.Delete(cookie);
                    }
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // Keycloak stores roles in realm_access.roles (nested JSON).
                // Extract them and map to standard role claims so that
                // [Authorize(Roles="admin")] and AuthorizeView work correctly.
                var identity = (ClaimsIdentity?)context.Principal?.Identity;
                if (identity == null) return Task.CompletedTask;

                var realmAccessClaim = identity.FindFirst("realm_access");
                if (realmAccessClaim != null)
                {
                    using var doc = JsonDocument.Parse(realmAccessClaim.Value);
                    if (doc.RootElement.TryGetProperty("roles", out var roles))
                    {
                        foreach (var role in roles.EnumerateArray())
                        {
                            var roleValue = role.GetString();
                            if (!string.IsNullOrEmpty(roleValue))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                            }
                        }
                    }
                }
                return Task.CompletedTask;
            },
            OnRemoteFailure = context =>
            {
                // Clear all stale OIDC cookies on failure to break potential loops
                foreach (var cookie in context.Request.Cookies.Keys)
                {
                    if (cookie.StartsWith(".AspNetCore.Correlation.") || cookie.StartsWith("OpenIdConnect.Nonce."))
                    {
                        context.Response.Cookies.Delete(cookie);
                    }
                }
                context.Response.Redirect("/");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            options.DetailedErrors = true;
        }
    });

// Register typed HttpClient pointing to WebApi via Aspire service discovery
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri("http://webapi");
});

// ── Controller for culture switching ──────────────────────
builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TokenProvider>();

var app = builder.Build();

// ── Request Localization Middleware ────────────────────────
var supportedCultures = new[] { new CultureInfo("hu"), new CultureInfo("en") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("hu"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
    // Only use Cookie + QueryString providers so the browser Accept-Language
    // header does not override the default Hungarian culture.
    RequestCultureProviders =
    [
        new Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider(),
        new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider()
    ]
});

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapStaticAssets();

// Auth endpoints — must be registered BEFORE MapRazorComponents
// so Blazor's client-side router doesn't intercept them.
app.MapGet("/login", (string? returnUrl) =>
{
    var redirect = string.IsNullOrWhiteSpace(returnUrl) ? "/" : $"/{returnUrl.TrimStart('/')}";
    return TypedResults.Challenge(new AuthenticationProperties { RedirectUri = redirect });
});

app.MapPost("/logout", () =>
{
    return TypedResults.SignOut(new AuthenticationProperties { RedirectUri = "/" },
        new[] { OpenIdConnectDefaults.AuthenticationScheme, CookieAuthenticationDefaults.AuthenticationScheme });
});

app.MapGet("/register", (string? returnUrl) =>
{
    var props = new AuthenticationProperties { RedirectUri = returnUrl ?? "/" };
    props.SetParameter("prompt", "create");
    return TypedResults.Challenge(props);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

===
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using DuskOfCoding.WebUi.Components;
using DuskOfCoding.WebUi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Kestrel header limit bump removed — no longer needed once cookie
// accumulation is fixed by the CookiePolicy + path-aware cleanup below.

builder.AddServiceDefaults();

// ── Localization ───────────────────────────────────────────
builder.Services.AddLocalization();

// ── Dev certificate trust (Aspire service-to-service) ─────
if (builder.Environment.IsDevelopment())
{
    builder.Services.ConfigureHttpClientDefaults(http =>
    {
        http.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
    });
}

// ── Server-side Session Cache ──────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITicketStore, MemoryCacheTicketStore>();

// ── Authentication (Keycloak OIDC) ───────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
    ? CookieSecurePolicy.SameAsRequest 
    : CookieSecurePolicy.Always;
    options.CookieManager = new ChunkingCookieManager();
})
// Wire ITicketStore via PostConfigure so it runs AFTER AddCookie's own Configure,
// guaranteeing SessionStore is set without calling BuildServiceProvider().
.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<ITicketStore>((options, store) =>
    {
        options.SessionStore = store;
    });

builder.Services.AddAuthentication()  // no-op re-registration, just need the builder chain
.AddKeycloakOpenIdConnect(
    "keycloak",
    realm: "DuskOfCoding",
    configureOptions: options =>
    {
        options.ClientId = "webui";
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.RequireHttpsMetadata = false;
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            RoleClaimType = ClaimTypes.Role,
            ValidateIssuer = true,
            ValidIssuers = new[]
            {
                // Browser-facing URL — what Keycloak stamps as "iss" in the JWT:
                "http://localhost:8080/realms/DuskOfCoding",
                // Aspire service-discovery URI:
                "https+http://keycloak/realms/DuskOfCoding",
                // Container fallback:
                "http://keycloak:8080/realms/DuskOfCoding",
            }
        };

        // ── Diagnostic OIDC Events + Keycloak role mapping + path-aware cookie cleanup ──
        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                Console.WriteLine($"[OIDC] ──── REDIRECT TO IDP ────");
                Console.WriteLine($"[OIDC] IssuerAddress: {context.ProtocolMessage.IssuerAddress}");
                Console.WriteLine($"[OIDC] RedirectUri:   {context.ProtocolMessage.RedirectUri}");
                Console.WriteLine($"[OIDC] State:         {context.ProtocolMessage.State}");

                // Log and clean up stale OIDC cookies (path-aware)
                var staleCookies = context.Request.Cookies.Keys
                    .Where(c => c.StartsWith(".AspNetCore.Correlation.") || c.StartsWith("OpenIdConnect.Nonce."))
                    .ToList();

                Console.WriteLine($"[OIDC] OIDC cookies present: {staleCookies.Count}");
                foreach (var c in staleCookies)
                    Console.WriteLine($"[OIDC]   {c}");

                if (staleCookies.Count > 3)
                {
                    foreach (var cookie in staleCookies.Take(staleCookies.Count - 2))
                    {
                        // Path-aware deletion to actually remove the cookie
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
                return Task.CompletedTask;
            },

            OnMessageReceived = context =>
            {
                Console.WriteLine($"[OIDC] ──── MESSAGE RECEIVED (callback) ────");
                Console.WriteLine($"[OIDC] Code:  {context.ProtocolMessage.Code?[..Math.Min(20, context.ProtocolMessage.Code?.Length ?? 0)]}...");
                Console.WriteLine($"[OIDC] Error: {context.ProtocolMessage.Error}");
                Console.WriteLine($"[OIDC] ErrorDescription: {context.ProtocolMessage.ErrorDescription}");
                Console.WriteLine($"[OIDC] State: {context.ProtocolMessage.State}");
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

                    // ── Keycloak role mapping (realm_access.roles → ClaimTypes.Role) ──
                    var realmAccessClaim = identity.FindFirst("realm_access");
                    if (realmAccessClaim != null)
                    {
                        using var doc = JsonDocument.Parse(realmAccessClaim.Value);
                        if (doc.RootElement.TryGetProperty("roles", out var roles))
                        {
                            foreach (var role in roles.EnumerateArray())
                            {
                                var roleValue = role.GetString();
                                if (!string.IsNullOrEmpty(roleValue))
                                {
                                    identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                                    Console.WriteLine($"[OIDC]   Mapped Keycloak role → {roleValue}");
                                }
                            }
                        }
                    }
                }

                if (context.Properties?.Items is { } items)
                {
                    Console.WriteLine($"[OIDC] Properties ({items.Count}):");
                    foreach (var prop in items)
                        Console.WriteLine($"[OIDC]   {prop.Key} = {prop.Value?[..Math.Min(80, prop.Value?.Length ?? 0)]}");
                }

                return Task.CompletedTask;
            },

            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[OIDC] ──── AUTHENTICATION FAILED ────");
                Console.WriteLine($"[OIDC] Exception Type:    {context.Exception.GetType().Name}");
                Console.WriteLine($"[OIDC] Exception Message: {context.Exception.Message}");
                if (context.Exception.InnerException != null)
                    Console.WriteLine($"[OIDC] Inner: {context.Exception.InnerException.Message}");
                return Task.CompletedTask;
            },

            OnRemoteFailure = context =>
            {
                Console.WriteLine($"[OIDC] ──── REMOTE FAILURE ────");
                Console.WriteLine($"[OIDC] Failure.Message: {context.Failure?.Message}");
                if (context.Failure?.InnerException != null)
                    Console.WriteLine($"[OIDC] Failure.Inner: {context.Failure.InnerException.Message}");

                // Check for protocol-level errors (invalid_grant, etc.)
                var error = context.HttpContext.Request.Query["error"].FirstOrDefault();
                var errorDesc = context.HttpContext.Request.Query["error_description"].FirstOrDefault();
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"[OIDC] Protocol Error:       {error}");
                    Console.WriteLine($"[OIDC] Protocol Description: {errorDesc}");
                }

                // Path-aware cleanup to prevent 431 cookie buildup
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
    });

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            options.DetailedErrors = true;
        }
    });

// Register typed HttpClient pointing to WebApi via Aspire service discovery
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri("http://webapi");
});

// ── Controller for culture switching ──────────────────────
builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TokenProvider>();

var app = builder.Build();

// ── Request Localization Middleware ────────────────────────
var supportedCultures = new[] { new CultureInfo("hu"), new CultureInfo("en") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("hu"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
    // Only use Cookie + QueryString providers so the browser Accept-Language
    // header does not override the default Hungarian culture.
    RequestCultureProviders =
    [
        new Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider(),
        new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider()
    ]
});

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// ── Cookie Policy (Phase 1 fix: correlation cookie SameSite) ──
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,
    OnAppendCookie = ctx =>
    {
        if (ctx.CookieOptions.SameSite == SameSiteMode.None && !ctx.Context.Request.IsHttps)
            ctx.CookieOptions.SameSite = SameSiteMode.Unspecified;
    },
    OnDeleteCookie = ctx =>
    {
        if (ctx.CookieOptions.SameSite == SameSiteMode.None && !ctx.Context.Request.IsHttps)
            ctx.CookieOptions.SameSite = SameSiteMode.Unspecified;
    }
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapStaticAssets();

// Auth endpoints — must be registered BEFORE MapRazorComponents
// so Blazor's client-side router doesn't intercept them.
app.MapGet("/login", (string? returnUrl) =>
{
    var redirect = string.IsNullOrWhiteSpace(returnUrl) ? "/" : $"/{returnUrl.TrimStart('/')}";
    return TypedResults.Challenge(new AuthenticationProperties { RedirectUri = redirect });
});

app.MapPost("/logout", () =>
{
    return TypedResults.SignOut(new AuthenticationProperties { RedirectUri = "/" },
        new[] { OpenIdConnectDefaults.AuthenticationScheme, CookieAuthenticationDefaults.AuthenticationScheme });
});

app.MapGet("/register", (string? returnUrl) =>
{
    var props = new AuthenticationProperties { RedirectUri = returnUrl ?? "/" };
    props.SetParameter("prompt", "create");
    return TypedResults.Challenge(props);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

```

---

### [AppHost.cs](file:///d:/Repos/PracticePlatform/DuskOfCoding.AppHost/AppHost.cs)

Removed `.WithEnvironment("QUARKUS_HTTP_LIMITS_MAX_HEADER_SIZE", "128k")` — no longer needed.

```diff:AppHost.cs
var builder = DistributedApplication.CreateBuilder(args);

// RabbitMQ broker — managed container with management UI
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

// Execution API — standalone sandbox service
var executionApi = builder.AddProject<Projects.DuskOfCoding_ExecutionApi>("executionapi")
    .WithHttpHealthCheck("/health");

var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume()
    .WithRealmImport("../KeycloakConfig")
    .WithEnvironment("QUARKUS_HTTP_LIMITS_MAX_HEADER_SIZE", "128k");

// Web API — main onboarding app, calls Execution API via service discovery
var webApi = builder.AddProject<Projects.DuskOfCoding_WebApi>("webapi")
    .WithHttpHealthCheck("/health")
    .WithReference(executionApi)
    .WithReference(messaging)
    .WithReference(keycloak)
    .WaitFor(messaging)
    .WaitFor(keycloak);

// Web UI — Blazor Server frontend, calls Web API via service discovery
builder.AddProject<Projects.DuskOfCoding_WebUi>("webui")
    .WithHttpHealthCheck("/health")
    .WithReference(webApi)
    .WithReference(keycloak)
    .WaitFor(keycloak);

// Tutor Worker — MCP-enabled background worker consuming from RabbitMQ
builder.AddProject<Projects.DuskOfCoding_TutorWorker>("tutorworker")
    .WithReference(messaging)
    .WithReference(executionApi)
    .WaitFor(messaging);

builder.Build().Run();


===
var builder = DistributedApplication.CreateBuilder(args);

// RabbitMQ broker — managed container with management UI
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

// Execution API — standalone sandbox service
var executionApi = builder.AddProject<Projects.DuskOfCoding_ExecutionApi>("executionapi")
    .WithHttpHealthCheck("/health");

var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume()
    .WithRealmImport("../KeycloakConfig");

// Web API — main onboarding app, calls Execution API via service discovery
var webApi = builder.AddProject<Projects.DuskOfCoding_WebApi>("webapi")
    .WithHttpHealthCheck("/health")
    .WithReference(executionApi)
    .WithReference(messaging)
    .WithReference(keycloak)
    .WaitFor(messaging)
    .WaitFor(keycloak);

// Web UI — Blazor Server frontend, calls Web API via service discovery
builder.AddProject<Projects.DuskOfCoding_WebUi>("webui")
    .WithHttpHealthCheck("/health")
    .WithReference(webApi)
    .WithReference(keycloak)
    .WaitFor(keycloak);

// Tutor Worker — MCP-enabled background worker consuming from RabbitMQ
builder.AddProject<Projects.DuskOfCoding_TutorWorker>("tutorworker")
    .WithReference(messaging)
    .WithReference(executionApi)
    .WaitFor(messaging);

builder.Build().Run();


```

---

## Build Verification

Both `DuskOfCoding.WebUi` and `DuskOfCoding.AppHost` build with **0 warnings, 0 errors**.

## Manual Verification Checklist (Pending)

- [ ] Start via `dotnet run` from AppHost, wait for all services healthy
- [ ] Login as `student` → verify `[OIDC] TOKEN VALIDATED` in console with correct claims
- [ ] Login as `admin` → verify admin badge and admin sections
- [ ] Logout → verify session cookie cleared
- [ ] DevTools → Cookies → no `.AspNetCore.Correlation.*` or `OpenIdConnect.Nonce.*` persist
- [ ] 5+ login/logout cycles → no HTTP 431, no cookie buildup
- [ ] No "WebSocket not in OPEN state" in browser console
