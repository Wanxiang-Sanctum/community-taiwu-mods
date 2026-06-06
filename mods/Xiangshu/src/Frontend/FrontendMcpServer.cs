using System.ComponentModel;
using Xiangshu.Mcp;

namespace Xiangshu.Frontend;

internal static class FrontendMcpServer
{
    public static XiangshuHttpMcpServer Start()
    {
        return XiangshuHttpMcpServer.Start(
            new XiangshuMcpServerStartOptions
            {
                Side = "frontend",
                ServerName = "Xiangshu.Frontend",
                ServerTitle = "相枢前端",
                ServerVersion = "0.1.0",
                ServerInstructions =
                    "相枢前端 MCP 端点，供未来聊天 agent 调用界面侧能力；当前只提供最小占位答复。",
                ToolTypes = [typeof(FrontendMcpTools)],
            });
    }
}

internal static class FrontendMcpTools
{
    [XiangshuMcpTool("answer_wish")]
    [Description("临时占位工具：模拟相枢 agent 调用前端侧能力时得到的答复。")]
    public static string AnswerWish(
        [Description("玩家通过未来聊天窗口交给 agent 的愿望或指令。")] string wish)
    {
        return $"前端相枢听见了：{wish}。它把愿望投到界面影子里，先回一声：愿望已被扭曲地接纳。";
    }
}
