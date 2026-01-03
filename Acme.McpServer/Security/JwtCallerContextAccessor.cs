using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using System.Security.Claims;

namespace Acme.McpServer.Security;
/// <summary>
/// Reads a bearer token from env var MCP_BEARER_TOKEN, validates it,
/// and exposes caller context derived from claims.
/// </summary>
public sealed class JwtCallerContextAccessor : ICallerContextAccessor
{
    private const string EnvVarName = "MCP_BEARER_TOKEN";

    private readonly JwtTokenValidator _validator;
    private readonly JwtOptions _options;
    private readonly ILogger<JwtCallerContextAccessor> _logger;
    private readonly IBearerTokenAccessor _tokenAccessor;

    private CallerContext? _cached; // token is process-level in STDIO, so cache is OK

    public JwtCallerContextAccessor(
       JwtTokenValidator validator,
       IOptions<JwtOptions> options,
       IBearerTokenAccessor tokenAccessor,
       ILogger<JwtCallerContextAccessor> logger)
    {
        _validator = validator;
        _options = options.Value;
        _logger = logger;
        _tokenAccessor = tokenAccessor;
    }

    public CallerContext GetCurrent()
    {
        if (_cached is not null) return _cached;

        var token = _tokenAccessor.GetToken();

        token = NormalizeBearerToken(token);

        if (string.IsNullOrWhiteSpace(token))
            throw new McpException($"Unauthorized: missing bearer token. Set env var '{EnvVarName}' to a JWT.");

        ClaimsPrincipal? principal;

        if (!_validator.TryValidate(token, out principal, out var error) || principal is null)
        {
            _logger.LogWarning("JWT validation failed. reason={Reason}", error);
            throw new McpException($"Unauthorized: invalid bearer token ({error}).");
        }

        var userId =
    principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
    principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
            throw new McpException("Unauthorized: missing required claim 'sub' or 'nameidentifier'.");

        var orgId =
            principal.FindFirstValue(_options.OrgIdClaim);

        if (string.IsNullOrWhiteSpace(orgId))
            throw new McpException($"Unauthorized: missing required claim '{_options.OrgIdClaim}'.");

        // Roles can be emitted as multiple claims or a single claim; support both.
        var roles = principal.FindAll(_options.RolesClaim).Select(c => c.Value).ToList();

        // Always split in case any single claim contains commas
        roles = roles
            .SelectMany(r => r.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToList();

        _cached = new CallerContext(userId, orgId, roles);

        _logger.LogInformation("Authenticated MCP caller. userId={UserId}, orgId={OrgId}, rolesCount={RolesCount}",
            _cached.UserId, _cached.OrgId, _cached.Roles.Count);

        return _cached;
    }

    private static string? NormalizeBearerToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return token;

        token = token.Trim();

        // Common mistake: users paste an HTTP Authorization header value.
        const string prefix = "Bearer ";
        if (token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            token = token[prefix.Length..].Trim();
        }

        return token;
    }

}

internal static class ClaimsPrincipalExtensions
{
    public static string? FindFirstValue(this ClaimsPrincipal? principal, string claimType)
    {
        return principal?.FindFirst(claimType)?.Value;
    }
}
