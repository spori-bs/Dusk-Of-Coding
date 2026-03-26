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
// Disabling DangerousAcceptAnyServerCertificateValidator using standard Aspire dev-certs.
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
        options.ClientSecret = "secret"; // Matches Keycloak config update
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.RequireHttpsMetadata = false;
        options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
        if (builder.Environment.IsDevelopment())
        {
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }
        else
        {
            options.BackchannelHttpHandler = new HttpClientHandler(); 
        }
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            RoleClaimType = "roles",
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

                    // ── Keycloak role mapping and Token Claims ──
                    // Role mapping is natively handled by Protocol Mappers in Keycloak + RoleClaimType = "roles".
                    // Securely preserve the access_token in a claim so Blazor Server can read it
                    // without leaking it to the HTML DOM via PersistentComponentState.
                    if (context.TokenEndpointResponse?.AccessToken is string accessToken)
                    {
                        identity.AddClaim(new Claim("access_token", accessToken));
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

