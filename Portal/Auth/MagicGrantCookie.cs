using System.Text.Json;
using System.Text;

namespace Portal.Auth;

public static class MagicGrantCookie
{
    public const string CookieName = "MagicGrant";

    public static string Encode(MagicGrant grant)
    {
        var json = JsonSerializer.Serialize(grant);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public static MagicGrant? Decode(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(value));
            return JsonSerializer.Deserialize<MagicGrant>(json);
        }
        catch
        {
            return null;
        }
    }
}
