using Acme.McpServer.InternalApi;
using Acme.McpServer.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var runHttp = args.Any(a => a.Equals("--http", StringComparison.OrdinalIgnoreCase));

if (!runHttp)
{
    // -------------------------
    // STDIO MODE (Claude Desktop local)
    // -------------------------
    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace); // stderr

    // Options
    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
    builder.Services.Configure<InternalApiOptions>(builder.Configuration.GetSection("InternalApi"));

    // Token source: env var for STDIO
    builder.Services.AddSingleton<IBearerTokenAccessor, EnvBearerTokenAccessor>();

    // Auth + caller context
    builder.Services.AddSingleton<JwtTokenValidator>();
    builder.Services.AddSingleton<ICallerContextAccessor, JwtCallerContextAccessor>();

    // HttpClient to internal API
    builder.Services.AddHttpClient<IInternalApiClient, InternalApiClient>((sp, http) =>
    {
        var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<InternalApiOptions>>().Value;
        http.BaseAddress = new Uri(opts.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    });

    // MCP server over STDIO
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();

    await builder.Build().RunAsync();
}
else
{
    // -------------------------
    // HTTP MODE (local now, container/AKS later)
    // -------------------------
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(); // normal web logging is fine

    // Options
    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
    builder.Services.Configure<InternalApiOptions>(builder.Configuration.GetSection("InternalApi"));

    // Needed to read headers in services
    builder.Services.AddHttpContextAccessor();

    // Token source: Authorization header
    builder.Services.AddSingleton<IBearerTokenAccessor, HttpHeaderBearerTokenAccessor>();

    // Auth + caller context
    builder.Services.AddSingleton<JwtTokenValidator>();
    builder.Services.AddSingleton<ICallerContextAccessor, JwtCallerContextAccessor>();

    // HttpClient to internal API
    builder.Services.AddHttpClient<IInternalApiClient, InternalApiClient>((sp, http) =>
    {
        var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<InternalApiOptions>>().Value;
        http.BaseAddress = new Uri(opts.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    });

    // MCP server over HTTP
    builder.Services
        .AddMcpServer()
        .WithHttpTransport()
        .WithToolsFromAssembly();

    var app = builder.Build();

    // Map MCP endpoint at /mcp
    app.MapMcp("/mcp");

    // Optional: health endpoint (useful for containers/AKS later)
    app.MapGet("/healthz", () => Results.Ok("ok"));

    await app.RunAsync("http://localhost:3004");
}
