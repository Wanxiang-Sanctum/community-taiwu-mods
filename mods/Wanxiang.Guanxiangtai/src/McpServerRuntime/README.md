# MCP Server Runtime 模块结构

`src/McpServerRuntime/` 是前端启动器和 MCP server 共享的 MCP server 外部入口协调模块。它不是 MCP server 进程本体；进程本体在
`src/McpServer/`。

这个模块只提供三类稳定能力：

- `GuanxiangtaiMcp`：Mod id、显示名、HTTP path、loopback host、脱离启动参数和可选稳定 token 的环境变量名。
- `GuanxiangtaiMcpPaths`：Mod 目录内运行目录和运行态入口文件路径。
- `McpServerEndpointRegistry` 与 `GuanxiangtaiMcpLocks`：MCP server 入口登记、live server 判断和并发锁。

合理调用方只有两个：

- `src/Frontend/`：作为游戏内启动器，判断是否有 live server，并在需要时提交脱离启动请求。
- `src/McpServer/`：作为游戏外服务本体，注册和释放自身 HTTP 入口。

后端插件不是这个模块的调用方。后端插件只参与 `src/Ipc/` 定义的 MCP server 内部游戏 IPC 契约，不读取或发布 MCP server 外部入口文件，
也不知道 agent 如何连接 MCP server。

运行态入口文件只记录仍有进程存活的 MCP server，包括进程 id、可执行路径和 HTTP 入口地址。它不定义 MCP 工具或脚本协议，
也不保存 token。前后端插件的内部 IPC 入口文件由 `src/Ipc/` 维护。

完整运行模型见 [../../docs/mcp-server-runtime.md](../../docs/mcp-server-runtime.md)。
