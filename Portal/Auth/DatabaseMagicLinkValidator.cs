using Infrastructure.Peristance;
using Microsoft.EntityFrameworkCore;
using Portal.Auth;
using Domain.Enums;

namespace Portal.Auth;

public class DatabaseMagicLinkValidator : IMagicLinkValidator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseMagicLinkValidator> _logger;

    public DatabaseMagicLinkValidator(IServiceScopeFactory scopeFactory, ILogger<DatabaseMagicLinkValidator> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<MagicGrant?> ValidateAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var link = await db.MagicLinks.FirstOrDefaultAsync(l => l.Token == token, ct);
            if (link == null)
            {
                _logger.LogWarning("Magic link token not found: {Token}", token);
                return null;
            }
            if (!link.IsValid())
            {
                _logger.LogWarning("Magic link invalid: {Id} Active:{Active} Used:{Used} Expired:{Expired}", link.Id, link.IsActive, link.IsUsed, link.ExpiresAt < DateTime.UtcNow);
                return null;
            }

            // Non lo marchiamo come usato qui per permettere sessioni persistenti; se single-use farlo qui.
            return new MagicGrant(
                link.LeagueId,
                link.SessionId ?? Guid.Empty,
                link.TeamId ?? Guid.Empty,
                link.Type == MagicLinkType.Spectator,
                link.ExpiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating magic link token {Token}", token);
            return null;
        }
    }
}
