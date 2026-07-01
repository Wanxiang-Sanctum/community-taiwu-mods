using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;
using Wanxiang.Xiangshu.Ipc;
using MsLogger = Microsoft.Extensions.Logging.ILogger;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
int parentProcessId = int.Parse(
    builder.Configuration["parent-pid"] ?? throw new InvalidOperationException("--parent-pid 是必需参数。"),
    CultureInfo.InvariantCulture);
string logFilePath = builder.Configuration["log-file"]
    ?? Path.Combine(AppContext.BaseDirectory, "Wanxiang.Xiangshu.McpServer.log");
string manifestFilePath = builder.Configuration["manifest-file"]
    ?? throw new InvalidOperationException("--manifest-file 是必需参数。");
string bearerToken = Environment.GetEnvironmentVariable(IpcRuntime.McpBearerTokenEnvironmentVariable)
    ?? throw new InvalidOperationException(
        IpcRuntime.McpBearerTokenEnvironmentVariable + " 环境变量是必需的。");
string? logDirectory = Path.GetDirectoryName(logFilePath);

if (string.IsNullOrWhiteSpace(bearerToken))
{
    throw new InvalidOperationException(
        IpcRuntime.McpBearerTokenEnvironmentVariable + " 环境变量不能为空。");
}

if (!string.IsNullOrEmpty(logDirectory))
{
    _ = Directory.CreateDirectory(logDirectory);
}

IpcEndpointRegistry.ConfigureManifestPath(manifestFilePath);

Serilog.Core.Logger fileLogger = CreateFileLogger(logFilePath);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(fileLogger, dispose: false);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);

builder.WebHost.ConfigureKestrel(
    options => options.Listen(IPAddress.Loopback, port: 0));

_ = builder.Services
    .AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<Wanxiang.Xiangshu.McpServer.PluginTools>(
        Wanxiang.Xiangshu.McpServer.McpToolJson.SerializerOptions);

MsLogger? logger = null;

try
{
    WebApplication app = builder.Build();
    logger = app.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Wanxiang.Xiangshu.McpServer");
    McpServerLog.Starting(logger);
    _ = app.Use(
        (context, next) => AuthorizeMcpRequestAsync(
            context,
            next,
            bearerToken));
    _ = app.MapMcp(IpcRuntime.McpPath);

    await app.StartAsync();

    Uri address = GetListeningAddress(app);
    IpcEndpoint endpoint = new()
    {
        Role = IpcRuntime.McpServerEndpointRole,
        Transport = IpcRuntime.McpTransportName,
        Host = IpcRuntime.LoopbackHost,
        Path = IpcRuntime.McpPath,
        Port = address.Port,
        ProcessId = Environment.ProcessId,
        StartedAtUtc = DateTimeOffset.UtcNow,
    };

    using IpcEndpointRegistration registration = IpcEndpointRegistry.Register(endpoint);
    McpServerLog.EndpointRegistered(logger);

    IHostApplicationLifetime lifetime = app.Lifetime;
    Task parentWatchTask = StopWhenParentExitsAsync(
        parentProcessId,
        lifetime,
        lifetime.ApplicationStopping,
        logger);

    await app.WaitForShutdownAsync();
    await parentWatchTask;
    McpServerLog.Stopped(logger);
}
catch (Exception ex)
{
    if (logger is null)
    {
        fileLogger.Fatal(
            ex,
            "MCP server 在宿主日志器可用前失败。");
    }
    else
    {
        McpServerLog.Failed(logger, ex);
    }

    throw;
}
finally
{
    await fileLogger.DisposeAsync();
}

static Uri GetListeningAddress(WebApplication app)
{
    IServer server = app.Services.GetRequiredService<IServer>();
    IServerAddressesFeature addressesFeature = server.Features.Get<IServerAddressesFeature>()!;

    return new Uri(addressesFeature.Addresses.Single());
}

static async Task StopWhenParentExitsAsync(
    int parentProcessId,
    IHostApplicationLifetime lifetime,
    CancellationToken cancellationToken,
    MsLogger logger)
{
    try
    {
        while (IsProcessRunning(parentProcessId))
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }
    catch (OperationCanceledException)
    {
        return;
    }

    McpServerLog.ParentExited(logger);
    lifetime.StopApplication();
}

static bool IsProcessRunning(int processId)
{
    try
    {
        using Process process = Process.GetProcessById(processId);
        return !process.HasExited;
    }
    catch (ArgumentException)
    {
        return false;
    }
}

static async Task AuthorizeMcpRequestAsync(
    HttpContext context,
    Func<Task> next,
    string bearerToken)
{
    if (context.Request.Path != IpcRuntime.McpPath)
    {
        await next();
        return;
    }

    if (!IsOriginAllowed(context.Request))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }

    if (!HasExpectedBearerToken(context.Request, bearerToken))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = "Bearer realm=\"xiangshu-mcp\"";
        return;
    }

    await next();
}

static bool IsOriginAllowed(HttpRequest request)
{
    if (!request.Headers.TryGetValue("Origin", out Microsoft.Extensions.Primitives.StringValues origins))
    {
        return true;
    }

    if (origins.Count != 1)
    {
        return false;
    }

    return IsLocalHttpOrigin(origins[0]);
}

static bool IsLocalHttpOrigin(string? origin)
{
    return !string.IsNullOrWhiteSpace(origin)
        && Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri)
        && string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        && IsLoopbackHost(uri.Host);
}

static bool IsLoopbackHost(string host)
{
    return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || (IPAddress.TryParse(host, out IPAddress? address) && IPAddress.IsLoopback(address));
}

static bool HasExpectedBearerToken(
    HttpRequest request,
    string expectedToken)
{
    if (!request.Headers.TryGetValue(
            IpcRuntime.McpAuthorizationHeaderName,
            out Microsoft.Extensions.Primitives.StringValues authorizationHeaders))
    {
        return false;
    }

    if (authorizationHeaders.Count != 1)
    {
        return false;
    }

    return MatchesBearerToken(authorizationHeaders[0], expectedToken);
}

static bool MatchesBearerToken(
    string? authorizationHeader,
    string expectedToken)
{
    if (!AuthenticationHeaderValue.TryParse(authorizationHeader, out AuthenticationHeaderValue? header)
        || !string.Equals(
            header.Scheme,
            IpcRuntime.BearerScheme,
            StringComparison.OrdinalIgnoreCase)
        || string.IsNullOrWhiteSpace(header.Parameter))
    {
        return false;
    }

    string actualToken = header.Parameter.Trim();
    byte[] actualBytes = Encoding.UTF8.GetBytes(actualToken);
    byte[] expectedBytes = Encoding.UTF8.GetBytes(expectedToken);

    return actualBytes.Length == expectedBytes.Length
        && CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
}

static Serilog.Core.Logger CreateFileLogger(string logFilePath)
{
    return new LoggerConfiguration()
        .WriteTo.File(
            new CompactJsonFormatter(),
            logFilePath,
            fileSizeLimitBytes: 1_048_576,
            rollOnFileSizeLimit: true,
            retainedFileCountLimit: 4)
        .CreateLogger();
}

internal static partial class McpServerLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "正在启动 MCP server。")]
    public static partial void Starting(MsLogger logger);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "MCP endpoint 已登记。")]
    public static partial void EndpointRegistered(MsLogger logger);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "父进程已退出，正在停止 MCP server。")]
    public static partial void ParentExited(MsLogger logger);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "MCP server 已停止。")]
    public static partial void Stopped(MsLogger logger);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Critical,
        Message = "MCP server 运行失败。")]
    public static partial void Failed(
        MsLogger logger,
        Exception exception);
}
