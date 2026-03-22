using System.Globalization;
using DuskOfCoding.WebUi.Components;
using DuskOfCoding.WebUi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

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

// ── Authentication (Keycloak OIDC) ───────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.CookieManager = new ChunkingCookieManager();
})
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
    });

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
builder.Services.AddTransient<AuthDelegatingHandler>();
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri("http://webapi");
})
.AddHttpMessageHandler<AuthDelegatingHandler>();

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
    return TypedResults.Challenge(new AuthenticationProperties { RedirectUri = returnUrl ?? "/" });
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

