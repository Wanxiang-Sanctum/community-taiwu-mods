# IPC 共享模块

`src/Ipc/` 是前端插件、后端插件和 MCP sidecar 共享的跨进程 contract 与本机 endpoint 辅助库。

职责：

- 定义 MessagePipe 请求/响应 DTO。
- 定义受信脚本执行请求、脚本输入，以及脚本运行响应的嵌套 MessagePack 判别联合：
  `notInvoked(reason)` 或 `invoked(returnValue | exception)`。
- 维护前端、后端和 MCP server 的 endpoint manifest 注册与发现；manifest 用 endpoint `role` 区分进程角色。
- 提供相枢运行目录、插件部署目录和本机 loopback endpoint 辅助方法。

这个模块只描述跨进程协议和共享基础设施。前端 UI、后端游戏逻辑、MCP 工具语义和本机 Agent 调用由对应
运行模块实现；脚本能访问哪些游戏 API 也由目标侧插件进程决定。

脚本执行本身归前端或后端 endpoint。`src/Ipc/` 不解释工具意图，不判断玩家目标是否达成，也不维护脚本编译
引用规则。
