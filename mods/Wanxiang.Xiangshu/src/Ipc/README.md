# IPC 共享模块

`src/Ipc/` 是前端插件、后端插件和 MCP sidecar 共享的 contract 与本机 endpoint 辅助库。

当前职责：

- 定义 MessagePipe 请求/响应 DTO。
- 维护前端、后端和 MCP server 的 endpoint manifest 注册与发现。
- 提供相枢运行目录解析和本机 loopback endpoint 辅助方法。

这个模块描述跨进程协议和共享基础设施。前端 UI、后端游戏逻辑、MCP 工具语义和本机 Agent 调用由对应
运行模块实现。

后续脚本运行应先在这里固化共享 contract，例如目标侧、运行 id、脚本输入、结果、错误和进展事件；真正
执行仍分别归前端或后端 endpoint。
