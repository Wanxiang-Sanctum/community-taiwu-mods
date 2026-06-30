# MCP Server 模块结构

`src/McpServer/` 是观象台的游戏外 MCP server 进程。它负责 HTTP MCP transport、鉴权、可见控制台输出和自身入口登记。
游戏前端插件会确保它启动；维护者也可以手动单独启动。正常启动不需要参数。

工具由 MCP server 对 agent 暴露，再通过内部 IPC/bridge 访问游戏侧插件；游戏进程自身不登记为 agent 可连接入口。

## 本模块负责

- 监听宿主侧 loopback 上的 Streamable HTTP `/mcp`。
- 从 `WANXIANG_GUANXIANGTAI_MCP_TOKEN` 读取 bearer token，并通过 ASP.NET Core 鉴权管线保护 `/mcp`。
- 读取 Mod 目录下可选的 `Guanxiangtai.Local.json`，只接受 `mcpServer.port` 作为端口覆盖。
- 在运行目录登记自身 HTTP 入口，并在可见终端窗口打印 URL 和 token 环境变量名。
- 持有按 Mod 目录派生的单实例锁，避免重复启动多个 server。
- 提供 `guanxiangtai_status` 只读工具，分别检测前端和后端插件是否能通过内部 IPC 响应状态请求。
- 提供 `guanxiangtai_run_csharp_script` 工具，把受信 C# 脚本请求转发到前端或后端 IPC endpoint。

## 外部边界

- MCP client 配置由使用者自己的 MCP client 管理。
- 默认 agent 工作区不是观象台 MCP server 的运行前提。
- 前端和后端游戏进程只通过内部 IPC 被本 server 调用；MCP 工具返回体不暴露内部 IPC 地址。
- token、agent 会话、脚本内容、调试会话和游戏进程 IPC 地址不写入本模块持久状态。

脚本运行中，MCP server 负责把工具调用路由到目标侧 IPC endpoint，把 `entryThread` 写入 IPC 请求，并把内部的嵌套判别联合整理为
Agent 可读 JSON：未调用入口时返回原因和可选诊断；已调用入口时再区分入口返回值和入口异常。实际线程切换由目标侧插件实现。

MCP server 不参与脚本编译引用解析，也不维护前后端 DLL 清单。入口契约和 Mod 侧响应映射归目标插件进程内的
`src/Scripting/`；通用编译、入口调用和运行事实归 shared 动态脚本运行核心；前端主线程分派和前端脚本引用选项创建归
`shared/Wanxiang.Taiwu.DynamicScripting.Frontend`，后端主循环分派归 `shared/Wanxiang.Taiwu.DynamicScripting.Backend`。
可部署依赖仍由打包项目维护。

包内运行时，server 会从 `Processes/Wanxiang.Guanxiangtai.McpServer/` 反推 Mod 目录。开发态直接运行项目时，server 输出目录作为运行目录的基准目录。
完整运行模型见 [../../docs/mcp-server-runtime.md](../../docs/mcp-server-runtime.md)。
