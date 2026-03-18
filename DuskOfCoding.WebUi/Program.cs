using System.Globalization;
using DuskOfCoding.WebUi.Components;
using DuskOfCoding.WebUi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ── Localization ───────────────────────────────────────────
builder.Services.AddLocalization();

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

app.UseAntiforgery();

app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

