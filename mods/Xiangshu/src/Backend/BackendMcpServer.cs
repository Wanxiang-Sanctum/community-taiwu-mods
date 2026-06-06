using System.ComponentModel;
using Xiangshu.Mcp;

namespace Xiangshu.Backend;

internal static class BackendMcpServer
{
    public static XiangshuHttpMcpServer Start()
    {
        return XiangshuHttpMcpServer.Start(
            new XiangshuMcpServerStartOptions
            {
                Side = "backend",
                ServerName = "Xiangshu.Backend",
                ServerTitle = "相枢后端",
                ServerVersion = "0.1.0",
                ServerInstructions =
                    "相枢后端 MCP 端点，供未来聊天 agent 调用规则与数据侧能力；当前只提供最小占位答复。",
                ToolTypes = [typeof(BackendMcpTools)],
            });
    }
}

internal static class BackendMcpTools
{
    [XiangshuMcpTool("answer_wish")]
    [Description("临时占位工具：模拟相枢 agent 调用后端侧能力时得到的答复。")]
    public static string AnswerWish(
        [Description("玩家通过未来聊天窗口交给 agent 的愿望或指令。")] string wish)
    {
        return $"后端相枢记录了：{wish}。它在数据深处回声：结果会到来，但路径未必仍像你所愿。";
    }
}
