using Microsoft.AspNetCore.Http;
using Domain.ValueObjects;

namespace Portal.Auth;

public interface IMagicGrantAccessor
{
    MagicGrant? Current { get; }
}

internal class MagicGrantAccessor : IMagicGrantAccessor
{
    private readonly IHttpContextAccessor _http;
    private MagicGrant? _cached;
    private bool _loaded;

    public MagicGrantAccessor(IHttpContextAccessor http)
    {
        _http = http;
    }

    public MagicGrant? Current
    {
        get
        {
            if (_loaded) return _cached;
            _loaded = true;
            var ctx = _http.HttpContext;
            if (ctx == null) return null;
            if (ctx.Request.Cookies.TryGetValue(MagicGrantCookie.CookieName, out var raw))
            {
                _cached = MagicGrantCookie.Decode(raw);
            }
            return _cached;
        }
    }
}