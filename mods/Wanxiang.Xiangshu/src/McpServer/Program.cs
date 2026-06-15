using System.Diagnostics;
using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
    builder.Configuration["parent-pid"] ?? throw new InvalidOperationException("--parent-pid is required."),
    CultureInfo.InvariantCulture);
string logFilePath = builder.Configuration["log-file"]
    ?? Path.Combine(AppContext.BaseDirectory, "Wanxiang.Xiangshu.McpServer.log");
string manifestFilePath = builder.Configuration["manifest-file"]
    ?? throw new InvalidOperationException("--manifest-file is required.");
string? logDirectory = Path.GetDirectoryName(logFilePath);

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
    .WithTools<Wanxiang.Xiangshu.McpServer.PluginTools>();

MsLogger? logger = null;

try
{
    WebApplication app = builder.Build();
    logger = app.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Wanxiang.Xiangshu.McpServer");
    McpServerLog.Starting(
        logger,
        Environment.ProcessId,
        parentProcessId,
        logFilePath);
    _ = app.MapMcp(IpcRuntime.McpPath);

    await app.StartAsync();

    Uri address = GetListeningAddress(app);
    IpcEndpoint endpoint = new()
    {
        Side = IpcRuntime.McpServerSide,
        Transport = IpcRuntime.McpTransportName,
        Host = IpcRuntime.LoopbackHost,
        Path = IpcRuntime.McpPath,
        Port = address.Port,
        ProcessId = Environment.ProcessId,
        StartedAtUtc = DateTimeOffset.UtcNow,
    };

    using IpcEndpointRegistration registration = IpcEndpointRegistry.Register(endpoint);
    if (logger.IsEnabled(LogLevel.Information))
    {
        string endpointAddress = IpcRuntime.FormatEndpointAddress(endpoint);
        string manifestPath = IpcEndpointRegistry.ManifestPath;

        McpServerLog.EndpointRegistered(
            logger,
            endpointAddress,
            endpoint.Port,
            manifestPath);
    }

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
            "MCP server failed before the host logger was available; processId={ProcessId}; parentProcessId={ParentProcessId}.",
            Environment.ProcessId,
            parentProcessId);
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

    McpServerLog.ParentExited(logger, parentProcessId);
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
        Message = "Starting MCP server; processId={ProcessId}; parentProcessId={ParentProcessId}; logFile={LogFile}.")]
    public static partial void Starting(
        MsLogger logger,
        int processId,
        int parentProcessId,
        string logFile);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "MCP endpoint registered; address={Address}; port={Port}; manifest={ManifestPath}.")]
    public static partial void EndpointRegistered(
        MsLogger logger,
        string address,
        int port,
        string manifestPath);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Parent process exited; parentProcessId={ParentProcessId}; stopping MCP server.")]
    public static partial void ParentExited(
        MsLogger logger,
        int parentProcessId);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "MCP server stopped.")]
    public static partial void Stopped(MsLogger logger);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Critical,
        Message = "MCP server failed.")]
    public static partial void Failed(
        MsLogger logger,
        Exception exception);
}
