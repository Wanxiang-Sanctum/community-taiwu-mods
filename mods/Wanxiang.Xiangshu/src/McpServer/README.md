# MCP Server 模块结构

`src/McpServer/` 是游戏外 MCP sidecar。前端插件负责启动它；它启动后注册自己的 MCP endpoint，并把事件
日志写入 `.xiangshu-runtime/Diagnostics/McpServer/`。

MCP 工具承担前后端 C# 脚本执行路由，以及把中间答复请求转发到前端聊天会话。endpoint 可用性随工具调用
结果返回。

主对话协议归前端投递会话，长期上下文归本机 Agent 会话。脚本运行中，MCP server 负责把 MCP 工具调用
路由到目标侧 IPC endpoint，并把入口返回值、错误和诊断整理为 Agent 可读的工具返回。

MCP server 不参与脚本编译引用解析，也不维护前后端 DLL 清单；这些责任分别归目标插件进程内的
`src/Scripting/` 运行器和打包项目。
