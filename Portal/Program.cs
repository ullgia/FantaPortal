using Portal.Components;
using Infrastructure.Services;
using Portal.Services;
using Infrastructure;
using Application;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


// Layered services
builder.Services.AddInfrastructureServices(builder.Configuration)
                .AddApplicationServices()
                .AddPortalServices();

var app = builder.Build();

// Apply database migrations and seeding
await app.ApplyMigrationsAsync();
await app.SeedDatabaseAsync();

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

// Configure all application endpoints
app.ConfigureApplicationEndpoints();

app.Run();
