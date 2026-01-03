namespace Acme.McpServer.Security;

/// <summary>
/// Represents the authenticated caller.
/// Later this will be populated from JWT claims.
/// </summary>
public sealed record CallerContext(
    string UserId,
    string OrgId,
    IReadOnlyCollection<string> Roles);

