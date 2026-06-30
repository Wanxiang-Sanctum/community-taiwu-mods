return {
  Title = "观象台：面向 Mod 制作者的本机 MCP 服务",
  Source = 0,
  FileId = 0,
  Version = "0.1.0",
  GameVersion = "1.0.44",
  Author = "Wanxiang Sanctum",
  Description = [=[观象台是面向太吾 Mod 制作者的本机 MCP 服务入口。

启用后，游戏前端会确保包内 MCP server 进程启动。该进程使用可见终端窗口，提供本机 Streamable HTTP MCP 入口，不随游戏进程退出而关闭，也可以单独启动。

当前版本提供 MCP server 启动、鉴权、入口定位，以及前后端插件状态检测工具。后续动态代码执行、游戏启停和调试能力会通过 MCP server 暴露，再由 server 内部路由到游戏侧能力。]=],
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
