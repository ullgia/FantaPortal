using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Infrastructure.Peristance;

namespace Infrastructure.Services;

public interface IMagicLinkService
{
    Task<MagicLink> CreateParticipantLinkAsync(Guid leagueId, Guid teamId, string participantName, TimeSpan? validFor = null);
    Task<MagicLink> CreateSpectatorLinkAsync(Guid leagueId, string spectatorName, TimeSpan? validFor = null);
    Task<MagicLink> CreateAdminLinkAsync(Guid leagueId, string adminName, TimeSpan? validFor = null);
    Task<MagicLink?> ValidateLinkAsync(string token, string ipAddress);
    Task<bool> DeactivateLinkAsync(string token);
    Task<bool> DeactivateAllLinksForLeagueAsync(Guid leagueId);
    Task<List<MagicLink>> GetActiveLinksForLeagueAsync(Guid leagueId);
    Task<bool> AssignSessionToLinksAsync(Guid leagueId, Guid sessionId);
    Task CleanupExpiredLinksAsync();
}

public class MagicLinkService : IMagicLinkService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MagicLinkService> _logger;

    public MagicLinkService(IServiceScopeFactory scopeFactory, ILogger<MagicLinkService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<MagicLink> CreateParticipantLinkAsync(
        Guid leagueId, 
        Guid teamId, 
        string participantName, 
        TimeSpan? validFor = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Validate that the league and team exist
        var league = await db.Leagues.FindAsync(leagueId);
        if (league == null) throw new DomainException("League not found");

        var team = await db.LeaguePlayers.FindAsync(teamId);
        if (team == null || team.LeagueId != leagueId) 
            throw new DomainException("Team not found in this league");

        // Deactivate existing links for this team
        var existingLinks = await db.MagicLinks
            .Where(l => l.LeagueId == leagueId && l.TeamId == teamId && l.IsActive)
            .ToListAsync();

        foreach (var link in existingLinks)
        {
            link.Deactivate();
        }

        var magicLink = MagicLink.CreateForParticipant(leagueId, teamId, participantName, validFor);
        db.MagicLinks.Add(magicLink);
        await db.SaveChangesAsync();

        _logger.LogInformation("Created participant magic link for team {TeamId} in league {LeagueId}", 
            teamId, leagueId);

        return magicLink;
    }

    public async Task<MagicLink> CreateSpectatorLinkAsync(
        Guid leagueId, 
        string spectatorName, 
        TimeSpan? validFor = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Validate that the league exists
        var league = await db.Leagues.FindAsync(leagueId);
        if (league == null) throw new DomainException("League not found");

        var magicLink = MagicLink.CreateForSpectator(leagueId, spectatorName, validFor);
        db.MagicLinks.Add(magicLink);
        await db.SaveChangesAsync();

        _logger.LogInformation("Created spectator magic link for league {LeagueId}", leagueId);

        return magicLink;
    }

    public async Task<MagicLink> CreateAdminLinkAsync(
        Guid leagueId, 
        string adminName, 
        TimeSpan? validFor = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Validate that the league exists
        var league = await db.Leagues.FindAsync(leagueId);
        if (league == null) throw new DomainException("League not found");

        // Deactivate existing admin links for security
        var existingAdminLinks = await db.MagicLinks
            .Where(l => l.LeagueId == leagueId && l.Type == MagicLinkType.Admin && l.IsActive)
            .ToListAsync();

        foreach (var link in existingAdminLinks)
        {
            link.Deactivate();
        }

        var magicLink = MagicLink.CreateForAdmin(leagueId, adminName, validFor);
        db.MagicLinks.Add(magicLink);
        await db.SaveChangesAsync();

        _logger.LogInformation("Created admin magic link for league {LeagueId}", leagueId);

        return magicLink;
    }

    public async Task<MagicLink?> ValidateLinkAsync(string token, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        if (string.IsNullOrWhiteSpace(ipAddress)) ipAddress = "Unknown";

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var magicLink = await db.MagicLinks
            .FirstOrDefaultAsync(l => l.Token == token);

        if (magicLink == null) 
        {
            _logger.LogWarning("Magic link validation failed: token not found {Token}", token);
            return null;
        }

        if (!magicLink.IsValid())
        {
            _logger.LogWarning("Magic link validation failed: invalid link {LinkId} - Active: {Active}, Used: {Used}, Expired: {Expired}", 
                magicLink.Id, magicLink.IsActive, magicLink.IsUsed, magicLink.ExpiresAt < DateTime.UtcNow);
            return null;
        }

        // Mark as used (for single-use scenarios, you might want to make this configurable)
        magicLink.Use(ipAddress);
        await db.SaveChangesAsync();

        _logger.LogInformation("Magic link used successfully: {LinkId} by IP {IpAddress}", 
            magicLink.Id, ipAddress);

        return magicLink;
    }

    public async Task<bool> DeactivateLinkAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var magicLink = await db.MagicLinks
            .FirstOrDefaultAsync(l => l.Token == token);

        if (magicLink == null) return false;

        magicLink.Deactivate();
        await db.SaveChangesAsync();

        _logger.LogInformation("Magic link deactivated: {LinkId}", magicLink.Id);
        return true;
    }

    public async Task<bool> DeactivateAllLinksForLeagueAsync(Guid leagueId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var activeLinks = await db.MagicLinks
            .Where(l => l.LeagueId == leagueId && l.IsActive)
            .ToListAsync();

        if (!activeLinks.Any()) return false;

        foreach (var link in activeLinks)
        {
            link.Deactivate();
        }

        await db.SaveChangesAsync();

        _logger.LogInformation("Deactivated {Count} magic links for league {LeagueId}", 
            activeLinks.Count, leagueId);

        return true;
    }

    public async Task<List<MagicLink>> GetActiveLinksForLeagueAsync(Guid leagueId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.MagicLinks
            .Where(l => l.LeagueId == leagueId && l.IsActive && !l.IsUsed && l.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> AssignSessionToLinksAsync(Guid leagueId, Guid sessionId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var activeLinks = await db.MagicLinks
            .Where(l => l.LeagueId == leagueId && l.IsActive && !l.SessionId.HasValue)
            .ToListAsync();

        if (!activeLinks.Any()) return false;

        foreach (var link in activeLinks)
        {
            link.AssignToSession(sessionId);
        }

        await db.SaveChangesAsync();

        _logger.LogInformation("Assigned session {SessionId} to {Count} magic links for league {LeagueId}", 
            sessionId, activeLinks.Count, leagueId);

        return true;
    }

    public async Task CleanupExpiredLinksAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var expiredLinks = await db.MagicLinks
            .Where(l => l.IsActive && l.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        if (!expiredLinks.Any()) return;

        foreach (var link in expiredLinks)
        {
            link.Deactivate();
        }

        await db.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} expired magic links", expiredLinks.Count);
    }
}
