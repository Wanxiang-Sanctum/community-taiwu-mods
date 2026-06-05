using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Xiangshu.BackendAgentTools;

public sealed class XiangshuBackendAgentToolServer : IDisposable
{
    private readonly CancellationTokenSource _stopping = new();
    private readonly IHost _host;
    private readonly Task _serverTask;

    private XiangshuBackendAgentToolServer(IHost host)
    {
        _host = host;
        _serverTask = Task.Run(() => _host.RunAsync(_stopping.Token));
    }

    public static XiangshuBackendAgentToolServer Start()
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
                            Name = "Xiangshu.Backend",
                            Title = "相枢后端",
                            Version = "0.1.0",
                        };
                        options.ServerInstructions =
                            "相枢后端 MCP 端点，供未来聊天 agent 调用规则与数据侧能力；当前只提供最小占位答复。";
                    })
                    .WithStdioServerTransport()
                    .WithTools([typeof(XiangshuBackendAgentTools)]);
            })
            .Build();

        return new XiangshuBackendAgentToolServer(host);
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

internal static class XiangshuBackendAgentTools
{
    [McpServerTool(Name = "answer_wish")]
    [Description("临时占位工具：模拟相枢 agent 调用后端侧能力时得到的答复。")]
    internal static string AnswerWish(
        [Description("玩家通过未来聊天窗口交给 agent 的愿望或指令。")] string wish)
    {
        return $"后端相枢记录了：{wish}。它在数据深处回声：结果会到来，但路径未必仍像你所愿。";
    }
}
