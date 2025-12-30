using Acme.McpServer;
using Acme.McpServer.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Claude Desktop may launch this process with a working directory that is NOT the DLL directory.
// Use the assembly base directory as content root so appsettings.json is found reliably.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// IMPORTANT for STDIO: send logs to stderr, not stdout (stdout is reserved for JSON-RPC).
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o =>
{
    o.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenValidator>();



//builder.Services
//    .AddSingleton<ICallerContextAccessor, DevCallerContextAccessor>();
builder.Services.AddSingleton<ICallerContextAccessor, JwtCallerContextAccessor>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(); // discovers tools via attributes in this assembly

await builder.Build().RunAsync();
