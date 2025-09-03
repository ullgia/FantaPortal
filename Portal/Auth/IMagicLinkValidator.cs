namespace Portal.Auth;

public interface IMagicLinkValidator
{
    Task<MagicGrant?> ValidateAsync(string token, CancellationToken ct = default);
}
