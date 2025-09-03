namespace Portal.Auth;

public sealed record MagicGrant(
    Guid LeagueId,
    Guid? SessionId,
    Guid? TeamId,
    bool IsGuest,
    DateTime ExpiresUtc
);
