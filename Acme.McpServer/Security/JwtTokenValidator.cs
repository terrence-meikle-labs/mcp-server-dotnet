using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Acme.McpServer.Security;
public sealed class JwtTokenValidator
{
    private readonly JwtOptions _options;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenValidator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public ClaimsPrincipal Validate(string jwt)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_options.SigningKey);
        var validation = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,

            ValidateAudience = true,
            ValidAudience = _options.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        return _handler.ValidateToken(jwt, validation, out _);
    }

    public bool TryValidate(string jwt, out ClaimsPrincipal? principal, out string? error)
    {
        try
        {
            principal = Validate(jwt);
            error = null;
            return true;
        }
        catch (SecurityTokenExpiredException)
        {
            principal = null;
            error = "token expired";
            return false;
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            principal = null;
            error = $"invalid audience (expected '{_options.Audience}')";
            return false;
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            principal = null;
            error = $"invalid issuer (expected '{_options.Issuer}')";
            return false;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            principal = null;
            error = "invalid signature (signing key mismatch?)";
            return false;
        }
        catch (SecurityTokenException)
        {
            principal = null;
            error = "invalid token";
            return false;
        }
    }
}
