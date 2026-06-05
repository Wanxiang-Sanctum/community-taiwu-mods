using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using TaiwuModdingLib.Core.Plugin;

namespace Xiangshu.Frontend;

[PluginConfig("Xiangshu.Frontend", "WanxiangSanctum", "0.1.0")]
public sealed class FrontendPlugin : TaiwuRemakePlugin
{
    private XiangshuFrontendMcpServer? _mcpServer;

    public override void Initialize()
    {
        _mcpServer = XiangshuFrontendMcpServer.Start();
    }

    public override void Dispose()
    {
        _mcpServer?.Dispose();
        _mcpServer = null;
    }
}

internal sealed class XiangshuFrontendMcpServer : IDisposable
{
    private readonly CancellationTokenSource _stopping = new();
    private readonly IHost _host;
    private readonly Task _serverTask;

    private XiangshuFrontendMcpServer(IHost host)
    {
        _host = host;
        _serverTask = Task.Run(() => _host.RunAsync(_stopping.Token));
    }

    public static XiangshuFrontendMcpServer Start()
    {
        IHost host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                _ = logging.ClearProviders();
                _ = logging.AddConsole(options =>
                    options.LogToStandardErrorThreshold = LogLevel.Trace);
            })
            .ConfigureServices(services =>
            {
                _ = services
                    .AddMcpServer(options =>
                    {
                        options.ServerInfo = new Implementation
                        {
                            Name = "Xiangshu.Frontend",
                            Title = "相枢前端",
                            Version = "0.1.0",
                        };
                        options.ServerInstructions =
                            "相枢前端 MCP 端点，供未来聊天 agent 调用界面侧能力；当前只提供最小占位答复。";
                    })
                    .WithStdioServerTransport()
                    .WithTools([typeof(XiangshuFrontendTools)]);
            })
            .Build();

        return new XiangshuFrontendMcpServer(host);
    }

    public void Dispose()
    {
        _stopping.Cancel();

        try
        {
            _host.StopAsync().GetAwaiter().GetResult();
            _serverTask.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _host.Dispose();
            _stopping.Dispose();
        }
    }
}

internal static class XiangshuFrontendTools
{
    [McpServerTool(Name = "answer_wish")]
    [Description("临时占位工具：模拟相枢 agent 调用前端侧能力时得到的答复。")]
    internal static string AnswerWish(
        [Description("玩家通过未来聊天窗口交给 agent 的愿望或指令。")] string wish)
    {
        return $"前端相枢听见了：{wish}。它把愿望投到界面影子里，先回一声：愿望已被扭曲地接纳。";
    }
}
