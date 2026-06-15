# IPC 共享模块

`src/Ipc/` 是前端插件、后端插件和 MCP sidecar 共享的 contract 与本机 endpoint 辅助库。

职责：

- 定义 MessagePipe 请求/响应 DTO。
- 定义受信脚本执行请求、脚本输入、入口返回值、错误和诊断 DTO。
- 维护前端、后端和 MCP server 的 endpoint manifest 注册与发现；manifest 用 endpoint `role` 区分进程角色。
- 提供相枢运行目录解析和本机 loopback endpoint 辅助方法。

这个模块描述跨进程协议和共享基础设施。前端 UI、后端游戏逻辑、MCP 工具语义和本机 Agent 调用由对应
运行模块实现。

脚本执行本身分别归前端或后端 endpoint；这个模块只保留共享 contract 和 endpoint 基础设施。
