using Acme.Contracts;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Very basic stub. In real life you'd validate JWT etc.
// We'll at least parse it to prove the token is being forwarded.
app.MapGet("/org/summary", (HttpRequest req) =>
{
    var auth = req.Headers.Authorization.ToString();
    var sub = "unknown";
    var orgId = "unknown";

    if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        var token = auth["Bearer ".Length..].Trim();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? sub;
        orgId = jwt.Claims.FirstOrDefault(c => c.Type == "orgId")?.Value ?? orgId;
    }

    return Results.Ok(new OrgSummary(
        OrgId: orgId,
        OrgName: "Stub Org",
        ActiveUsers: 123,
        OpenItems: 9
    ));
});

app.MapGet("/items/search", (string query, int page = 1, int pageSize = 10) =>
{
    var all = Enumerable.Range(1, 200).Select(i => $"item-{i}").Where(x => x.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

    return Results.Ok(new PagedResult<string>(items, page, pageSize, all.Count));
});

app.Run();
