namespace Acme.McpServer.Security;
public interface ICallerContextAccessor
{
    CallerContext GetCurrent();
}
