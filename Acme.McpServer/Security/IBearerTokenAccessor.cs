namespace Acme.McpServer.Security;
public interface IBearerTokenAccessor
{
    string? GetToken();
}
