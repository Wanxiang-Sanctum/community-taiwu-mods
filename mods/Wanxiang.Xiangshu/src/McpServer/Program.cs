using System.Diagnostics;
using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Wanxiang.Xiangshu.Ipc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
int parentProcessId = int.Parse(
    builder.Configuration["parent-pid"]!,
    CultureInfo.InvariantCulture);

builder.WebHost.ConfigureKestrel(
    options => options.Listen(IPAddress.Loopback, port: 0));

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();

WebApplication app = builder.Build();
app.MapMcp(IpcRuntime.McpPath);

await app.StartAsync().ConfigureAwait(false);

Uri address = GetListeningAddress(app);

using IpcEndpointRegistration registration = IpcEndpointRegistry.Register(
    new IpcEndpoint
    {
        Side = IpcRuntime.McpServerSide,
        Transport = IpcRuntime.McpTransportName,
        Host = IpcRuntime.LoopbackHost,
        Path = IpcRuntime.McpPath,
        Port = address.Port,
        ProcessId = Environment.ProcessId,
        StartedAtUtc = DateTimeOffset.UtcNow,
    });

IHostApplicationLifetime lifetime = app.Lifetime;
Task parentWatchTask = StopWhenParentExitsAsync(
    parentProcessId,
    lifetime,
    lifetime.ApplicationStopping);

await app.WaitForShutdownAsync().ConfigureAwait(false);
await parentWatchTask.ConfigureAwait(false);

static Uri GetListeningAddress(WebApplication app)
{
    IServer server = app.Services.GetRequiredService<IServer>();
    IServerAddressesFeature addressesFeature = server.Features.Get<IServerAddressesFeature>()!;

    return new Uri(addressesFeature.Addresses.Single());
}

static async Task StopWhenParentExitsAsync(
    int parentProcessId,
    IHostApplicationLifetime lifetime,
    CancellationToken cancellationToken)
{
    try
    {
        while (IsProcessRunning(parentProcessId))
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
        }
    }
    catch (OperationCanceledException)
    {
        return;
    }

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
