using Acme.McpServer.InternalApi;
using Acme.Contracts;
using Acme.McpServer.Security;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Acme.McpServer.Tools;

[McpServerToolType]
public sealed class OrgTools
{
    private readonly ICallerContextAccessor _callerContext;
    private readonly IInternalApiClient _api;
    private readonly ILogger<OrgTools> _logger;

    public OrgTools(ICallerContextAccessor callerContext, IInternalApiClient api, ILogger<OrgTools> logger)
    {
        _callerContext = callerContext;
        _api = api;
        _logger = logger;
    }

    [McpServerTool, Description("Returns a summary for the caller's organization.")]
    public async Task<OrgSummary> GetMyOrgSummary(CancellationToken ct)
    {
        var caller = _callerContext.GetCurrent();
        _logger.LogInformation("Tool call: GetMyOrgSummary userId={UserId} orgId={OrgId}", caller.UserId, caller.OrgId);

        return await _api.GetMyOrgSummaryAsync(ct);
    }

    [McpServerTool, Description("Searches items in the caller's organization.")]
    public async Task<PagedResult<string>> SearchItems(
        [Description("Search text")] string query,
        [Description("Page number (1-based)")] int page = 1,
        [Description("Page size (max 50)")] int pageSize = 10,
        CancellationToken ct = default)
    {
        var caller = _callerContext.GetCurrent();
        _logger.LogInformation("Tool call: SearchItems userId={UserId} orgId={OrgId} page={Page} pageSize={PageSize}",
            caller.UserId, caller.OrgId, page, pageSize);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        return await _api.SearchItemsAsync(query, page, pageSize, ct);
    }
}
