namespace Acme.McpServer.Models;
public sealed record OrgSummary(
    string OrgId,
    string OrgName,
    int ActiveUsers,
    int OpenItems);
