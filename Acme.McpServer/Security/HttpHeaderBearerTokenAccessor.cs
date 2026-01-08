using Microsoft.AspNetCore.Http;

namespace Acme.McpServer.Security;
public sealed class HttpHeaderBearerTokenAccessor : IBearerTokenAccessor
{

    private readonly IHttpContextAccessor _http;

    public HttpHeaderBearerTokenAccessor(IHttpContextAccessor http)
    {
        _http = http;
    }
    public string? GetToken()
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return null;

        var auth = ctx.Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return auth["Bearer ".Length..].Trim();

        return null;
    }
}
