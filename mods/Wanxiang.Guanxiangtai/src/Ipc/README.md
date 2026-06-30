# IPC 共享模块

`src/Ipc/` 是观象台 MCP server 调用游戏前端、后端插件时共享的内部 MessagePipe 契约和 endpoint manifest 读写模块。

本模块只承载 MCP server 到目标插件端的内部路由，不把前端或后端游戏进程暴露成 agent 可连接入口。MCP client 仍只连接
MCP server 的 HTTP `/mcp`。

当前 IPC 只定义状态检测：

- 前端和后端插件各自启动一个 `messagepipe-tcp` loopback endpoint，并登记到 `.guanxiangtai-runtime/ipc-endpoints.json`。
- MCP server 读取 manifest 后，向对应 role 发送状态请求。
- 目标侧只确认能完成状态请求；MCP server 不把 OS 进程存活性当作对外状态事实。

状态检测语义保持窄边界：某侧 `available` 结果只表示 MCP server 已经通过内部 IPC 收到该侧状态响应。
