using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Acme.McpServer.Tools;
[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the provided message back to the caller. Safe read-only demo tool.")]
    public static string Echo([Description("Any text to echo back.")] string message)
       => $"echo: {message}";
}
