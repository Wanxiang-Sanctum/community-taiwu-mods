return {
  Title = "观象台：Mod 制作者的 MCP 入口",
  Source = 0,
  FileId = 0,
  Version = "0.1.0",
  GameVersion = "1.0.44",
  Author = "Wanxiang Sanctum",
  Description = [=[[h1]观象台[/h1]
[b]给熟悉 agent 与 MCP 的 Mod 制作者使用：让 agent 把当前游戏运行时纳入自己的工作循环。[/b]

观象台随 Mod 分发一个 HTTP MCP server。agent 连接这个入口后，可以按任务需要请求动态执行使用者信任的 C# 编译单元，读取、判断或改动游戏前端、后端进程中的运行时状态。太吾启停、重启和状态检测都是围绕这条运行时桥梁提供的辅助能力。

[hr][/hr]

[h2]工作方式[/h2]
[list]
[*]游戏前端会确保 MCP server 进程启动；也可以手动运行包内可执行文件。
[*]agent 只连接 MCP server；server 再把请求转给前端或后端插件。
[*]需要稳定入口时，可用 Mod 目录下的 Guanxiangtai.Local.json 定制 MCP server 端口。
[*]脚本按 agent 当前目标动态执行，结果回到 agent，用于继续分析、修改或验证。
[/list]

[h2]连接[/h2]
启动后从 server 窗口、固定端口配置或 .guanxiangtai-runtime/ 读取地址；在 MCP client 中注册 http://127.0.0.1:<port>/mcp，并使用同一个 bearer token。

固定端口配置写在 Mod 目录下的 Guanxiangtai.Local.json；文件缺失或 port 为 0 时使用随机空闲端口。

[code]{
  "mcpServer": {
    "port": 37617
  }
}[/code]

[code]<MOD_DIR>\Processes\Wanxiang.Guanxiangtai.McpServer\Wanxiang.Guanxiangtai.McpServer.exe[/code]

[h2]边界[/h2]
[list]
[*]不设置远程服务器；默认监听 127.0.0.1。
[*]C# 脚本在目标侧游戏进程内完全信任运行，不提供沙箱。
[/list]

[h2]详细说明[/h2]
[url=https://github.com/Wanxiang-Sanctum/community-taiwu-mods/tree/main/mods/Wanxiang.Guanxiangtai]https://github.com/Wanxiang-Sanctum/community-taiwu-mods/tree/main/mods/Wanxiang.Guanxiangtai[/url]
]=],
  Cover = "Cover.png",
  WorkshopCover = "Cover.png",
  TagList = {
    "Extensions",
  },
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
