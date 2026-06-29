return {
  Title = "观象台：面向 Mod 制作者的本机 MCP 服务",
  Source = 0,
  FileId = 0,
  Version = "0.1.0",
  GameVersion = "1.0.44",
  Author = "Wanxiang Sanctum",
  Description = [=[观象台是面向太吾 Mod 制作者的本机 MCP 服务入口。

启用后，游戏前端会确保包内 MCP server 进程启动。该进程使用可见终端窗口，提供本机 Streamable HTTP MCP 入口，不随游戏进程退出而关闭，也可以单独启动。

当前版本只提供 MCP server 启动、鉴权和入口定位，尚未提供可调用的游戏 tools。后续动态代码执行、游戏启停和调试能力会通过 MCP server 暴露，再由 server 内部路由到游戏侧能力。]=],
  FrontendPlugins = {
    "Wanxiang.Guanxiangtai.Frontend.dll",
  },
}
