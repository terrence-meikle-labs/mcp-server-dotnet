using Acme.Contracts;
using Acme.McpServer.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Acme.McpServer.InternalApi;
public sealed class InternalApiClient : IInternalApiClient
{
    private readonly HttpClient _http;
    private readonly IBearerTokenAccessor _tokenAccessor;
    private readonly InternalApiOptions _opts;
    private readonly ILogger<InternalApiClient> _logger;

    public InternalApiClient(
        HttpClient http,
        IBearerTokenAccessor tokenAccessor,
        IOptions<InternalApiOptions> opts,
        ILogger<InternalApiClient> logger)
    {
        _http = http;
        _tokenAccessor = tokenAccessor;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task<OrgSummary> GetMyOrgSummaryAsync(CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/org/summary");

        AttachBearer(req);

        _logger.LogInformation("Calling internal API: GET /org/summary");
        var resp = await _http.SendAsync(req, ct);

        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<OrgSummary>(cancellationToken: ct))!;
    }

    public async Task<PagedResult<string>> SearchItemsAsync(string query, int page, int pageSize, CancellationToken ct)
    {
        // Guardrail: cap page size here too (defense in depth)
        pageSize = Math.Min(pageSize, _opts.MaxPageSize);

        var url = $"/items/search?query={Uri.EscapeDataString(query)}&page={page}&pageSize={pageSize}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);

        AttachBearer(req);

        _logger.LogInformation("Calling internal API: GET {Url}", url);
        var resp = await _http.SendAsync(req, ct);

        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<PagedResult<string>>(cancellationToken: ct))!;
    }

    private void AttachBearer(HttpRequestMessage req)
    {
        var token = _tokenAccessor.GetToken();
        if (!string.IsNullOrWhiteSpace(token))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
