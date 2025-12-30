using Acme.McpServer.Models;
using Acme.McpServer.Security;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Acme.McpServer.Tools;

[McpServerToolType]
public sealed class OrgTools
{
    private readonly ICallerContextAccessor _callerContext;
    private readonly ILogger<OrgTools> _logger;

    public OrgTools(ICallerContextAccessor callerContext, ILogger<OrgTools> logger)
    {
        _callerContext = callerContext;
        _logger = logger;
    }

    [McpServerTool, Description("Returns a summary for the caller's organization.")]
    public OrgSummary GetMyOrgSummary()
    {
        var caller = _callerContext.GetCurrent();
        _logger.LogInformation("Tool call: GetMyOrgSummary userId={UserId} orgId={OrgId}", caller.UserId, caller.OrgId);

        // Mock data for now — later this calls a real internal API
        return new OrgSummary(
            OrgId: caller.OrgId,
            OrgName: "Acme Corp (Dev)",
            ActiveUsers: 42,
            OpenItems: 7
        );
    }

    [McpServerTool, Description("Searches items in the caller's organization.")]
    public PagedResult<string> SearchItems(
    [Description("Search text")] string query,
    [Description("Page number (1-based)")] int page = 1,
    [Description("Page size (max 50)")] int pageSize = 10)
    {
        const int MaxPageSize = 50;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var caller = _callerContext.GetCurrent();
        _logger.LogInformation("Tool call: SearchItems userId={UserId} orgId={OrgId} query={Query} page={Page} pageSize={PageSize}",
            caller.UserId, caller.OrgId, query, page, pageSize);

        // Fake data scoped to org
        var allItems = Enumerable.Range(1, 123)
            .Select(i => $"{caller.OrgId}-item-{i}")
            .Where(i => i.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var items = allItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<string>(
            Items: items,
            Page: page,
            PageSize: pageSize,
            TotalCount: allItems.Count
        );
    }
}
