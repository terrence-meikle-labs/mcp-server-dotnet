using Acme.Contracts;

namespace Acme.McpServer.InternalApi;
public interface IInternalApiClient
{
    Task<OrgSummary> GetMyOrgSummaryAsync(CancellationToken ct);
    Task<PagedResult<string>> SearchItemsAsync(string query, int page, int pageSize, CancellationToken ct);
}
