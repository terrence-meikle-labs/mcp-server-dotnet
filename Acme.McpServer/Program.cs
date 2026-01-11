using Acme.McpServer.InternalApi;
using Acme.McpServer.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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

    // The MCP HTTP endpoint expects JSON-RPC messages. If a client sends invalid JSON/JSON-RPC,
    // the underlying handler may throw (which would otherwise surface as a 500).
    // Translate those to a clean 400 response.
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (JsonException) when (context.Request.Path.StartsWithSegments("/mcp"))
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync("{\"error\":\"Invalid JSON-RPC request\"}");
        }
    });

    // Map MCP endpoint at /mcp
    app.MapMcp("/mcp");

    app.MapGet("/healthz", () => Results.Ok("ok"));

    // In containers, bind via ASPNETCORE_URLS (e.g. http://0.0.0.0:3004).
    // For local runs, default to localhost:3004.
    var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    await app.RunAsync(string.IsNullOrWhiteSpace(urls) ? "http://localhost:3004" : null);
}
