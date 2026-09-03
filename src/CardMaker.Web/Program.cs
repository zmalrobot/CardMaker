using System.Threading.RateLimiting;
using CardMaker.Domain.Identity;
using CardMaker.Infrastructure;
using CardMaker.Infrastructure.Identity;
using CardMaker.Infrastructure.Persistence;
using CardMaker.Web.Components;
using CardMaker.Web.Components.Account;
using CardMaker.Web.Endpoints;
using CardMaker.Web.Middleware;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Fail-fast security check (ADR-009): CardMaker.Web must never run with desktop bypass
if (string.Equals(builder.Configuration["AppMode"], "Desktop", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("ERRORE CRITICO DI SICUREZZA: CardMaker.Web non puo' essere avviato in modalita' Desktop.");
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddLocalization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole(AppRoles.Admin));

var dataRoot = DependencyInjection.ResolveDataRoot(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddCardMakerInfrastructure(builder.Configuration, dataRoot);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Password.RequiredLength = 12;
    })
    .AddRoles<IdentityRole>()
    .AddCardMakerIdentityStores()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddScoped<CardMaker.Application.Common.ILoadingService, CardMaker.UI.Services.LoadingService>();
builder.Services.AddScoped<CardMaker.Application.Assets.IAssetUriService, CardMaker.Infrastructure.Assets.WebAssetUriService>();

// Health Checks
builder.Services.AddHealthChecks();

// Rate Limiting (Protezione da DoS sul rendering e brute force)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetSlidingWindowLimiter(
            clientIp,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 20,
            });
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

// Security Headers & Pipeline
app.UseCardMakerSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

var supportedCultures = new[] { "it", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("it")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
app.UseRequestLocalization(localizationOptions);

app.UseRateLimiter();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(CardMaker.UI.Pages.Admin.AssetLibrary).Assembly);

app.MapHealthChecks("/healthz");

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();
app.MapAssetEndpoints();

app.MapGet("/SetCulture", (string culture, string? redirectUri, HttpContext context) =>
{
    if (culture is "it" or "en")
    {
        context.Response.Cookies.Append(
            Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
            Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(new Microsoft.AspNetCore.Localization.RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });
    }

    return Results.LocalRedirect(string.IsNullOrEmpty(redirectUri) ? "/" : redirectUri);
});

app.Run();
