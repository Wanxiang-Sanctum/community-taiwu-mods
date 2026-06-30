# MCP Server 模块结构

`src/McpServer/` 是观象台的游戏外 MCP server 进程。它负责 HTTP MCP transport、鉴权、可见控制台输出和自身入口登记。
游戏前端插件会确保它启动；维护者也可以手动单独启动。正常启动不需要参数。

这个模块不直接暴露游戏进程。工具由 MCP server 对 agent 暴露，再通过内部 IPC/bridge 访问游戏侧插件。

## 本模块负责

- 监听宿主侧 loopback 上的 Streamable HTTP `/mcp`。
- 从 `WANXIANG_GUANXIANGTAI_MCP_TOKEN` 读取 bearer token，并通过 ASP.NET Core 鉴权管线保护 `/mcp`。
- 读取 Mod 目录下可选的 `Guanxiangtai.Local.json`，只接受 `mcpServer.port` 作为端口覆盖。
- 在运行目录登记自身 HTTP 入口，并在可见终端窗口打印 URL 和 token 环境变量名。
- 持有按 Mod 目录派生的单实例锁，避免重复启动多个 server。
- 提供 `guanxiangtai_status` 只读工具，分别检测前端和后端插件是否能通过内部 IPC 响应状态请求。

## 本模块不负责

- 不维护 MCP client 配置。
- 不创建或假设默认 agent 工作区。
- 不把前端或后端游戏进程登记为 agent 可连接入口，也不在 MCP 工具返回体中暴露内部 IPC 地址。
- 不保存 token、agent 会话、脚本内容、调试会话或游戏进程 IPC 地址。

包内运行时，server 会从 `Processes/Wanxiang.Guanxiangtai.McpServer/` 反推 Mod 目录。开发态直接运行项目时，server 输出目录作为运行目录的基准目录。
完整运行模型见 [../../docs/mcp-server-runtime.md](../../docs/mcp-server-runtime.md)。
