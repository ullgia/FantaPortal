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
            // I magic link sono sempre validi fino a disabilitazione manuale dal master
            // Non controlliamo più la scadenza automatica
            return Task.FromResult<MagicGrant?>(grant);
        }
        return Task.FromResult<MagicGrant?>(null);
    }

    // Metodo per disabilitare manualmente un token (chiamato dal master)
    public static void DisableToken(string token) => _tokens.TryRemove(token, out _);

    // Metodo per ottenere tutti i token attivi (per il master)
    public static IEnumerable<(string Token, MagicGrant Grant)> GetActiveTokens() => 
        _tokens.Select(kvp => (kvp.Key, kvp.Value));
}
