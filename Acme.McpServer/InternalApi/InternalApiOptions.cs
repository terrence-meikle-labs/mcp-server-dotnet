namespace Acme.McpServer.InternalApi;
public sealed class InternalApiOptions
{
    public string BaseUrl { get; init; } = "";
    public int TimeoutSeconds { get; init; } = 10;
    public int MaxPageSize { get; init; } = 50;
}
