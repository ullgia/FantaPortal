using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public class MagicLink : BaseEntity
{
    public string Token { get; private set; } = string.Empty;
    public Guid LeagueId { get; private set; }
    public Guid? SessionId { get; private set; }
    public Guid? TeamId { get; private set; }
    public MagicLinkType Type { get; private set; }
    public string ParticipantName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? UsedByIpAddress { get; private set; }
    public DateTime? UsedAt { get; private set; }

    private MagicLink() { }

    public static MagicLink CreateForParticipant(
        Guid leagueId, 
        Guid teamId, 
        string participantName, 
        TimeSpan? validFor = null)
    {
        if (leagueId == Guid.Empty) throw new DomainException("LeagueId required");
        if (teamId == Guid.Empty) throw new DomainException("TeamId required");
        if (string.IsNullOrWhiteSpace(participantName)) throw new DomainException("Participant name required");

        var validity = validFor ?? TimeSpan.FromHours(24);
        
        return new MagicLink
        {
            Token = GenerateSecureToken(),
            LeagueId = leagueId,
            TeamId = teamId,
            Type = MagicLinkType.Participant,
            ParticipantName = participantName.Trim(),
            ExpiresAt = DateTime.UtcNow.Add(validity)
        };
    }

    public static MagicLink CreateForSpectator(Guid leagueId, string spectatorName, TimeSpan? validFor = null)
    {
        if (leagueId == Guid.Empty) throw new DomainException("LeagueId required");
        if (string.IsNullOrWhiteSpace(spectatorName)) throw new DomainException("Spectator name required");

        var validity = validFor ?? TimeSpan.FromHours(24);
        
        return new MagicLink
        {
            Token = GenerateSecureToken(),
            LeagueId = leagueId,
            Type = MagicLinkType.Spectator,
            ParticipantName = spectatorName.Trim(),
            ExpiresAt = DateTime.UtcNow.Add(validity)
        };
    }

    public static MagicLink CreateForAdmin(Guid leagueId, string adminName, TimeSpan? validFor = null)
    {
        if (leagueId == Guid.Empty) throw new DomainException("LeagueId required");
        if (string.IsNullOrWhiteSpace(adminName)) throw new DomainException("Admin name required");

        var validity = validFor ?? TimeSpan.FromHours(8);
        
        return new MagicLink
        {
            Token = GenerateSecureToken(),
            LeagueId = leagueId,
            Type = MagicLinkType.Admin,
            ParticipantName = adminName.Trim(),
            ExpiresAt = DateTime.UtcNow.Add(validity)
        };
    }

    public bool IsValid()
    {
        // I magic link sono validi fino a disabilitazione manuale dal master
        // Non controllano più la scadenza automatica
        return IsActive && !IsUsed;
    }

    public bool IsValidWithExpiry()
    {
        // Metodo separato per controlli di scadenza se necessario
        return IsActive && !IsUsed && DateTime.UtcNow < ExpiresAt;
    }

    public void Use(string ipAddress)
    {
        if (!IsValid()) throw new DomainException("Magic link is not valid");
        
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        UsedByIpAddress = ipAddress;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void AssignToSession(Guid sessionId)
    {
        if (sessionId == Guid.Empty) throw new DomainException("SessionId required");
        SessionId = sessionId;
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
