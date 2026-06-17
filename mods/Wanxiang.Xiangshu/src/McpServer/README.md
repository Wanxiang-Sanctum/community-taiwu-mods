# MCP Server 模块结构

`src/McpServer/` 是游戏外 MCP sidecar。前端插件负责启动它；它启动后注册自己的 MCP endpoint，并把
生命周期事件写入 `.xiangshu-runtime/Diagnostics/McpServer/`。

MCP 工具承担两类路由：把 C# 脚本请求转发到前端或后端 IPC endpoint，以及把中间答复请求转发到前端聊天
会话。endpoint 可用性在工具调用时检查。

事件日志只确认 sidecar 启动、endpoint 注册、父进程退出、停止和失败，不承担 MCP 工具调用统计。

主对话协议归前端投递会话，长期上下文归本机 Agent 会话。脚本运行中，MCP server 负责把 MCP 工具调用
路由到目标侧 IPC endpoint，并把内部的嵌套判别联合整理为 Agent 可读 JSON：未调用入口时只有原因；已调用
入口时再区分入口返回值和入口异常。

MCP server 不参与脚本编译引用解析，也不维护前后端 DLL 清单；这些责任分别归目标插件进程内的
`src/Scripting/` 运行器和打包项目。
