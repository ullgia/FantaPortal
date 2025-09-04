using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Portal.Services;
using Portal.Hubs;
using Portal.Auth;

namespace Portal.Services;

public static class EndpointsConfigurationService
{
    /// <summary>
    /// Configura tutti gli endpoint dell'applicazione
    /// </summary>
    public static WebApplication ConfigureApplicationEndpoints(this WebApplication app)
    {
        // Identity endpoints
        app.MapAdditionalIdentityEndpoints();

        // SignalR hubs
        app.MapHub<AuctionHub>("/hubs/auction");

        // Magic link endpoints
        app.ConfigureMagicLinkEndpoints();

        return app;
    }

    /// <summary>
    /// Configura gli endpoint per i magic link
    /// </summary>
    private static WebApplication ConfigureMagicLinkEndpoints(this WebApplication app)
    {
        // Endpoint per partecipare all'asta
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

        // Endpoint per guardare l'asta come ospite
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

        return app;
    }
}
