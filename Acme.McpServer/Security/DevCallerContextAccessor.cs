namespace Acme.McpServer.Security;
public sealed class DevCallerContextAccessor : ICallerContextAccessor
{
    public CallerContext GetCurrent()
       => new(
           UserId: "dev-user-123",
           OrgId: "org-abc",
           Roles: new[] { "Reader" }
       );
}
