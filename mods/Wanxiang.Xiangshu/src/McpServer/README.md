# MCP Server 模块结构

`src/McpServer/` 是游戏外 MCP sidecar。前端插件负责启动它；它启动后注册自己的 MCP endpoint，并把
生命周期事件写入 `.xiangshu-runtime/Diagnostics/McpServer/`。

MCP endpoint 使用 Streamable HTTP stateless transport。sidecar 不维护 MCP 协议会话状态，也不使用
`MCP-Session-Id` 做请求归属判断；相枢 Mod 的聊天状态和可见片段投递归前端投递会话，更长程模型上下文归本地 Agent 会话。

sidecar 负责 `/mcp` 请求门禁。前端启动 sidecar 时通过环境变量传入本次运行的 MCP bearer token；所有
`/mcp` 请求必须用 `Authorization: Bearer ...` 携带该 token。sidecar 还会拒绝非本机 HTTP `Origin`，
用于收窄浏览器来源请求。

MCP 工具承担三类路由：把 C# 脚本请求转发到前端或后端 IPC endpoint，把中间答复请求转发到当前前端投递
会话，以及把玩家视图截图工具转发到前端 `PlayerView/` 边界并返回 MCP image content。endpoint 可用性在
工具调用时检查。

事件日志只确认 sidecar 启动、endpoint 注册、父进程退出、停止和失败，不承担 MCP 工具调用统计，也不记录
MCP bearer token。

脚本运行中，MCP server 负责把 MCP 工具调用路由到目标侧 IPC endpoint，把 `entryThread` 写入 IPC 请求，并把
内部的嵌套判别联合整理为 Agent 可读 JSON：未调用入口时返回原因和可选诊断；已调用入口时再区分入口返回值和入口异常。
实际线程切换由目标侧插件实现。

MCP server 不参与脚本编译引用解析，也不维护前后端 DLL 清单。共享编译引用规则归目标插件进程内的
`src/Scripting/` 运行器；目标侧需要额外程序集引用时，由对应插件宿主提供；可部署依赖仍由打包项目维护。
