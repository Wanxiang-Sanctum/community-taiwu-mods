# MCP Server 模块结构

`src/McpServer/` 是游戏外 MCP sidecar。前端插件负责启动它；它启动后注册自己的 MCP endpoint，并把事件
日志写入 `XiangshuRuntime/Diagnostics/McpServer/`。

当前 MCP 工具承担工具链诊断、前后端 IPC ping，以及把中间答复请求转发到前端聊天会话。

主对话协议归前端投递会话，长期上下文归本机 Agent 会话。后续脚本运行中，MCP server 负责把 MCP 工具
调用路由到目标侧 IPC endpoint，并把返回结果整理为 Agent 可读的工具返回。
