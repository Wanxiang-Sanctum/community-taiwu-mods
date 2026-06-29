using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using Wanxiang.Guanxiangtai.McpServerRuntime;
using Wanxiang.Guanxiangtai.McpServer;

string? modDirectory = ResolveModDirectory(AppContext.BaseDirectory);
string ownerDirectory = modDirectory ?? AppContext.BaseDirectory;
string runtimeDirectory = GuanxiangtaiMcpPaths.GetRuntimeDirectory(ownerDirectory);

_ = Directory.CreateDirectory(runtimeDirectory);
McpServerEndpointRegistry.ConfigureRuntimeDirectory(runtimeDirectory);

using IDisposable? instanceLock = GuanxiangtaiMcpLocks.TryAcquireServerInstance(ownerDirectory);
if (instanceLock is null)
{
    await WriteExistingEndpointAndExitAsync();
    return;
}

WebApplicationBuilder builder = WebApplication.CreateBuilder();
string configurationPath = McpServerSettings.GetLocalConfigurationPath(ownerDirectory);
McpServerSettings settings;
try
{
    _ = builder.Configuration.AddJsonFile(
        configurationPath,
        optional: true,
        reloadOnChange: false);
    settings = McpServerSettings.Load(
        builder.Configuration,
        configurationPath);
}
catch (Exception ex) when (IsConfigurationException(ex))
{
    await Console.Error.WriteLineAsync(ex.Message);
    Environment.ExitCode = 1;
    return;
}

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(
    options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    });
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);

builder.WebHost.ConfigureKestrel(
    options => options.Listen(IPAddress.Loopback, settings.Port));

_ = builder.Services.AddSingleton(TimeProvider.System);
_ = builder.Services.AddWebEncoders();
_ = builder.Services.AddAuthenticationCore(
    options =>
    {
        options.DefaultAuthenticateScheme = McpBearerAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = McpBearerAuthenticationDefaults.AuthenticationScheme;
        options.DefaultForbidScheme = McpBearerAuthenticationDefaults.AuthenticationScheme;
    });
_ = new AuthenticationBuilder(builder.Services)
    .AddScheme<McpBearerAuthenticationOptions, McpBearerAuthenticationHandler>(
        McpBearerAuthenticationDefaults.AuthenticationScheme,
        configureOptions: options => options.BearerToken = settings.BearerToken);
_ = builder.Services.AddAuthorization();

_ = builder.Services
    .AddMcpServer(
        options =>
        {
            options.ServerInfo = new Implementation
            {
                Name = "wanxiang.guanxiangtai",
                Title = GuanxiangtaiMcp.DisplayName,
                Version = GuanxiangtaiMcp.Version,
            };
            options.ServerInstructions =
                "观象台是面向太吾绘卷 Mod 制作者的本机 MCP 服务入口。当前只提供连接、鉴权和生命周期基础，尚无可调用的游戏 tools；"
                + "后续 tools 会由 MCP server 通过内部 IPC/bridge 访问游戏侧能力。";
        })
    .WithHttpTransport(options => options.Stateless = true)
    .AddAuthorizationFilters();

await using WebApplication app = builder.Build();
ILogger logger = app.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("Wanxiang.Guanxiangtai.McpServer");

_ = app.Use(RejectNonLocalOriginAsync);
_ = app.UseAuthentication();
_ = app.UseAuthorization();
_ = app.MapMcp(GuanxiangtaiMcp.HttpPath).RequireAuthorization();

try
{
    await app.StartAsync();
}
catch (IOException ex)
{
    McpServerLog.ListenerUnavailable(
        logger,
        ex,
        GuanxiangtaiMcp.LoopbackHost,
        settings.Port,
        GuanxiangtaiMcp.HttpPath);
    Environment.ExitCode = 1;
    return;
}

Uri address = GetListeningAddress(app);
string executablePath = GetCurrentExecutablePath();
using McpServerEndpointRegistration registration =
    McpServerEndpointRegistry.Register(
        new McpServerEndpoint
        {
            Host = GuanxiangtaiMcp.LoopbackHost,
            Path = GuanxiangtaiMcp.HttpPath,
            Port = address.Port,
            ProcessId = Environment.ProcessId,
            StartedAt = DateTimeOffset.UtcNow,
            ExecutablePath = executablePath,
        });

McpServerLog.Listening(
    logger,
    GuanxiangtaiMcp.LoopbackHost,
    address.Port,
    GuanxiangtaiMcp.HttpPath);
McpServerLog.Configuration(
    logger,
    settings.ConfigurationFileExists
        ? settings.ConfigurationPath
        : "missing; using a random free loopback port");
McpServerLog.Token(
    logger,
    GuanxiangtaiMcp.BearerTokenEnvironmentVariable);
McpServerLog.EndpointFile(
    logger,
    McpServerEndpointRegistry.EndpointFilePath);
McpServerLog.IndependentProcess(logger);
await WriteAccessInstructionsAsync(address);

await app.WaitForShutdownAsync();

static async Task WriteExistingEndpointAndExitAsync()
{
    McpServerEndpoint? endpoint =
        McpServerEndpointRegistry.TryGetLiveEndpoint();

    if (endpoint is null)
    {
        await Console.Error.WriteLineAsync("Wanxiang.Guanxiangtai MCP server is already starting.");
        return;
    }

    await Console.Out.WriteLineAsync(
        string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"Wanxiang.Guanxiangtai MCP server is already running at http://{endpoint.Host}:{endpoint.Port}{endpoint.Path}"));
    await Console.Out.WriteLineAsync(
        "Bearer token environment variable: " + GuanxiangtaiMcp.BearerTokenEnvironmentVariable);
}

static Uri GetListeningAddress(WebApplication app)
{
    IServer server = app.Services.GetRequiredService<IServer>();
    IServerAddressesFeature addressesFeature = server.Features.Get<IServerAddressesFeature>()!;

    return new Uri(addressesFeature.Addresses.Single());
}

static Task RejectNonLocalOriginAsync(
    HttpContext context,
    Func<Task> next)
{
    if (IsOriginAllowed(context.Request))
    {
        return next();
    }

    context.Response.StatusCode = StatusCodes.Status403Forbidden;
    return Task.CompletedTask;
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
        && IsLocalHttpHost(uri.Host);
}

static bool IsLocalHttpHost(string host)
{
    return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "host.docker.internal", StringComparison.OrdinalIgnoreCase)
        || (IPAddress.TryParse(host, out IPAddress? address) && IPAddress.IsLoopback(address));
}

static async Task WriteAccessInstructionsAsync(Uri address)
{
    await Console.Out.WriteLineAsync();
    await Console.Out.WriteLineAsync("MCP URL:");
    await Console.Out.WriteLineAsync(
        string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"  http://{GuanxiangtaiMcp.LoopbackHost}:{address.Port}{GuanxiangtaiMcp.HttpPath}"));
    await Console.Out.WriteLineAsync("Bearer token environment variable:");
    await Console.Out.WriteLineAsync("  " + GuanxiangtaiMcp.BearerTokenEnvironmentVariable);
}

static bool IsConfigurationException(Exception exception)
{
    return exception is ArgumentException
        or FormatException
        or IOException
        or InvalidDataException
        or InvalidOperationException
        or NotSupportedException
        or UnauthorizedAccessException;
}

[SuppressMessage(
    "Globalization",
    "CA1308:Normalize strings to uppercase",
    Justification = "Paths are compared with OrdinalIgnoreCase, not normalized for display.")]
static string? ResolveModDirectory(string baseDirectory)
{
    DirectoryInfo baseDirectoryInfo = new(Path.GetFullPath(baseDirectory));
    DirectoryInfo? processesDirectory = baseDirectoryInfo.Parent;
    DirectoryInfo? modDirectory = processesDirectory?.Parent;

    if (string.Equals(
            baseDirectoryInfo.Name,
            "Wanxiang.Guanxiangtai.McpServer",
            StringComparison.OrdinalIgnoreCase)
        && string.Equals(processesDirectory?.Name, "Processes", StringComparison.OrdinalIgnoreCase)
        && modDirectory is not null)
    {
        return modDirectory.FullName;
    }

    return null;
}

static string GetCurrentExecutablePath()
{
    if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
    {
        return Environment.ProcessPath;
    }

    using Process process = Process.GetCurrentProcess();
    string? modulePath = process.MainModule?.FileName;

    if (!string.IsNullOrWhiteSpace(modulePath))
    {
        return modulePath;
    }

    throw new InvalidOperationException("Current MCP server executable path is unavailable.");
}

internal static partial class McpServerLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Wanxiang.Guanxiangtai MCP server is listening at http://{Host}:{Port}{Path}")]
    public static partial void Listening(
        ILogger logger,
        string host,
        int port,
        string path);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Runtime endpoint file: {EndpointFilePath}")]
    public static partial void EndpointFile(
        ILogger logger,
        string endpointFilePath);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "This MCP server process is independent from the game process. Close this console to stop it.")]
    public static partial void IndependentProcess(ILogger logger);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "Wanxiang.Guanxiangtai MCP server could not listen at http://{Host}:{Port}{Path}. Close the process using that port and start this server again.")]
    public static partial void ListenerUnavailable(
        ILogger logger,
        Exception exception,
        string host,
        int port,
        string path);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Local configuration: {Configuration}")]
    public static partial void Configuration(
        ILogger logger,
        string configuration);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Information,
        Message = "Bearer token environment variable: {EnvironmentVariable}")]
    public static partial void Token(
        ILogger logger,
        string environmentVariable);
}
