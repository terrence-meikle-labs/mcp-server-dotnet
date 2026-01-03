namespace Acme.McpServer.Security;
public class EnvBearerTokenAccessor : IBearerTokenAccessor
{
    private const string EnvVarName = "MCP_BEARER_TOKEN";
    public string? GetToken() => Environment.GetEnvironmentVariable(EnvVarName);
}
