# MCP Server Runtime 模块结构

`src/McpServerRuntime/` 是前端启动器和 MCP server 共享的运行态协调模块。它不是 MCP server 进程本体；进程本体在
`src/McpServer/`。

这个模块只提供三类稳定能力：

- `GuanxiangtaiMcp`：Mod id、显示名、HTTP path、loopback host 和固定 token 环境变量名。
- `GuanxiangtaiMcpPaths`：Mod 目录内运行目录和运行态入口文件路径。
- `McpServerEndpointRegistry` 与 `GuanxiangtaiMcpLocks`：MCP server 入口登记、live server 判断和并发锁。

合理调用方只有两个：

- `src/Frontend/`：作为游戏内启动器，判断是否需要拉起 MCP server。
- `src/McpServer/`：作为游戏外服务本体，注册和释放自身 HTTP 入口。

后端插件不是这个模块的调用方。后续如果增加后端插件，它应只参与 MCP server 内部的游戏 IPC/bridge 契约，不应读取或发布运行态入口文件，
也不应知道 agent 如何连接 MCP server。

运行态入口文件只记录仍有进程存活的 MCP server，包括进程 id、可执行路径和 HTTP 入口地址。它不定义 MCP tools，不承载 agent 工作区，
也不保存工具语义、脚本协议、调试协议或游戏进程 IPC 地址。

完整运行模型见 [../../docs/mcp-server-runtime.md](../../docs/mcp-server-runtime.md)。
