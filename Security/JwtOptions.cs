namespace Acme.McpServer.Security;
public sealed class JwtOptions
{
    public string Issuer { get; init; } = "";
    public string Audience { get; init; } = "";
    public string SigningKey { get; init; } = "";
    public string OrgIdClaim { get; init; } = "orgId";
    public string RolesClaim { get; init; } = "roles";
}
