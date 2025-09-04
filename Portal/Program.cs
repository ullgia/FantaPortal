using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portal.Components;
using Portal.Components.Account;
using Portal.Data;
using Portal.Hubs;
using Portal.Services;
using Infrastructure;
using Application;
using Domain.Entities;
using Radzen;
using Portal.Auth;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// Use both regular DbContext and factory for different scenarios
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Layered services
builder.Services.AddInfrastructureServices(builder.Configuration)
                .AddApplicationServices();

// SignalR and realtime notifications
builder.Services.AddSignalR();
builder.Services.AddSingleton<Application.Services.IRealtimeNotificationService, SignalRRealtimeNotificationService>();
builder.Services.AddScoped<Portal.Services.IRealtimeNotificationService, Portal.Services.SignalRNotificationService>();
builder.Services.AddScoped<Portal.Services.AuctionHubClient>();
builder.Services.AddScoped<Portal.Services.AuctionRealtimeStore>();
builder.Services.AddRadzenComponents();

// Magic link validator (replace with real implementation later)
builder.Services.AddSingleton<IMagicLinkValidator, InMemoryMagicLinkValidator>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// SignalR hubs
app.MapHub<AuctionHub>("/hubs/auction");

// Magic link endpoints
app.MapGet("/join/{token}", async (HttpContext http, string token, IMagicLinkValidator validator) =>
{
    var grant = await validator.ValidateAsync(token, http.RequestAborted);
    if (grant is null)
    {
        return Results.Redirect("/error?code=invalid_token");
    }

    http.Response.Cookies.Append(MagicGrantCookie.CookieName, MagicGrantCookie.Encode(grant), new CookieOptions
    {
        HttpOnly = true,
        Secure = http.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Expires = grant.ExpiresUtc
    });

    var target = $"/app/partecipante/{grant.LeagueId}/{grant.SessionId}";
    return Results.Redirect(target);
});

app.MapGet("/watch/{token}", async (HttpContext http, string token, IMagicLinkValidator validator) =>
{
    var grant = await validator.ValidateAsync(token, http.RequestAborted);
    if (grant is null)
    {
        return Results.Redirect("/error?code=invalid_token");
    }

    // force guest flag
    var guestGrant = grant with { IsGuest = true };
    http.Response.Cookies.Append(MagicGrantCookie.CookieName, MagicGrantCookie.Encode(guestGrant), new CookieOptions
    {
        HttpOnly = true,
        Secure = http.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Expires = guestGrant.ExpiresUtc
    });

    var target = $"/app/ospite/{guestGrant.LeagueId}/{guestGrant.SessionId}";
    return Results.Redirect(target);
});

app.Run();
