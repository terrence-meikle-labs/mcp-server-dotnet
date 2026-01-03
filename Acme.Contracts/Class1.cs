namespace Acme.Contracts;

public sealed record OrgSummary(
    string OrgId,
    string OrgName,
    int ActiveUsers,
    int OpenItems);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
