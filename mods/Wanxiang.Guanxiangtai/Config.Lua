return {
  Title = "观象台：面向 Mod 制作者的本机 MCP 服务",
  Source = 0,
  FileId = 0,
  Version = "0.1.0",
  GameVersion = "1.0.44",
  Author = "Wanxiang Sanctum",
  Description = [=[观象台是面向太吾 Mod 制作者的本机 MCP 服务入口。

启用后，游戏前端会确保包内 MCP server 进程启动。该进程使用可见终端窗口，提供本机 Streamable HTTP MCP 入口，不随游戏进程退出而关闭，也可以单独启动。

当前版本提供 MCP server 启动、鉴权、HTTP 入口登记、前后端插件状态检测，以及受信 C# 脚本执行工具。脚本在目标插件进程内运行，不提供沙箱；游戏启停和调试能力如后续提供，仍会通过 MCP server 内部路由到游戏侧能力。]=],
  Dependencies = {
    [1] = 3747731025,
  },
  BackendPlugins = {
    "Backend/Wanxiang.Guanxiangtai.Backend.dll",
  },
  FrontendPlugins = {
    "Frontend/Wanxiang.Guanxiangtai.Frontend.dll",
  },
}
