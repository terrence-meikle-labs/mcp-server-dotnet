using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var issuer = "acme-dev-issuer";
var audience = "acme-mcp-dev";
var signingKey = "DEV_ONLY__replace_me_with_32chars_minimum!!"; // must match appsettings.json

var userId = "dev-user-123";
var orgId = "org-abc";

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

var claims = new List<Claim>
{
    new(JwtRegisteredClaimNames.Sub, userId),
    new("orgId", orgId),
    new("roles", "Reader")
};

var token = new JwtSecurityToken(
    issuer: issuer,
    audience: audience,
    claims: claims,
    notBefore: DateTime.UtcNow.AddMinutes(-1),
    expires: DateTime.UtcNow.AddHours(1),
    signingCredentials: creds);

var jwt = new JwtSecurityTokenHandler().WriteToken(token);
Console.WriteLine(jwt);
