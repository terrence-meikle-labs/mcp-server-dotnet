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
    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace); // stderr

    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
    builder.Services.Configure<InternalApiOptions>(builder.Configuration.GetSection("InternalApi"));

    builder.Services.AddSingleton<IBearerTokenAccessor, EnvBearerTokenAccessor>();

    builder.Services.AddSingleton<JwtTokenValidator>();
    builder.Services.AddSingleton<ICallerContextAccessor, JwtCallerContextAccessor>();

    builder.Services.AddHttpClient<IInternalApiClient, InternalApiClient>((sp, http) =>
    {
        var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<InternalApiOptions>>().Value;
        http.BaseAddress = new Uri(opts.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    });

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();

    await builder.Build().RunAsync();
}
else
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(); // normal web logging is fine

    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
    builder.Services.Configure<InternalApiOptions>(builder.Configuration.GetSection("InternalApi"));

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddSingleton<IBearerTokenAccessor, HttpHeaderBearerTokenAccessor>();

    builder.Services.AddSingleton<JwtTokenValidator>();
    builder.Services.AddSingleton<ICallerContextAccessor, JwtCallerContextAccessor>();

    builder.Services.AddHttpClient<IInternalApiClient, InternalApiClient>((sp, http) =>
    {
        var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<InternalApiOptions>>().Value;
        http.BaseAddress = new Uri(opts.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    });

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

    app.MapMcp("/mcp");

    app.MapGet("/healthz", () => Results.Ok("ok"));

    var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    await app.RunAsync(string.IsNullOrWhiteSpace(urls) ? "http://localhost:3004" : null);
}
