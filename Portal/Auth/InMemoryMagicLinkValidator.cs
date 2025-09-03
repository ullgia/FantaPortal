using System.Collections.Concurrent;

namespace Portal.Auth;

public class InMemoryMagicLinkValidator : IMagicLinkValidator
{
    private static readonly ConcurrentDictionary<string, MagicGrant> _tokens = new();

    public static void Seed(string token, MagicGrant grant) => _tokens[token] = grant;

    public Task<MagicGrant?> ValidateAsync(string token, CancellationToken ct = default)
    {
        if (_tokens.TryGetValue(token, out var grant))
        {
            if (DateTime.UtcNow <= grant.ExpiresUtc)
            {
                return Task.FromResult<MagicGrant?>(grant);
            }
        }
        return Task.FromResult<MagicGrant?>(null);
    }
}
