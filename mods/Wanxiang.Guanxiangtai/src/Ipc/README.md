# IPC 共享模块

`src/Ipc/` 是观象台 MCP server 调用游戏前端、后端插件时共享的内部 MessagePipe 契约和 endpoint manifest 读写模块。

本模块只承载 MCP server 到目标插件端的内部路由，不把前端或后端游戏进程暴露成 agent 可连接入口。MCP client 仍只连接
MCP server 的 HTTP `/mcp`。

本模块承载这些内部协议面：

- 前端和后端插件各自启动一个 `messagepipe-tcp` loopback endpoint，并登记到 `.guanxiangtai-runtime/ipc-endpoints.json`。
- MCP server 读取 manifest 后，向对应 role 发送状态请求。
- 目标侧只确认能完成状态请求；MCP server 不把 OS 进程存活性当作对外状态事实。
- MCP server 可以向前端发送游戏退出请求，供 `guanxiangtai_stop_taiwu(method=requestQuit)` 使用；前端收到请求后在 Unity 主线程设置
  `GameApp.ReadyToQuit` 并调用 `GameApp.QuitGame()`。该 IPC 契约只表示请求被前端处理，进程消失等待归 MCP server 生命周期工具。
- MCP server 也可以向指定 role 发送受信 C# 脚本执行请求，语义字段包含 `script`、`arguments` JSON 对象和
  `entryThread`。IPC DTO 以对象根的规范化 JSON 文本承载 `arguments`，不把它变成脚本可见的字符串参数。
- 脚本响应使用嵌套 MessagePack 判别联合，区分入口未调用、入口返回值和入口异常。

状态检测语义保持窄边界：某侧 `available` 结果只表示 MCP server 已经通过内部 IPC 收到该侧状态响应。

脚本入口契约和 Mod 侧响应映射归 `src/Scripting/`；实际编译、入口调用和通用运行事实归 shared 动态脚本运行核心。
目标侧可见 API 和显式引用配置归前端或后端插件宿主。IPC 模块只承载跨进程请求与响应。
