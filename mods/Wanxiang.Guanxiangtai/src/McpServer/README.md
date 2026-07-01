# MCP Server 模块结构

`src/McpServer/` 是观象台的游戏外 MCP server 进程。它负责 HTTP MCP transport、鉴权、可见控制台输出和自身入口登记。
游戏前端插件会确保它启动；维护者也可以手动单独启动。正常启动不需要参数。

MCP server 是 agent 的唯一可连接入口；需要游戏能力时，本模块再把工具请求转给当前游戏运行时里的前端或后端插件。

## 本模块负责

- 监听宿主侧 loopback 上的 Streamable HTTP `/mcp`。
- 使用 bearer token 保护 `/mcp`：优先读取 `WANXIANG_GUANXIANGTAI_MCP_TOKEN`；环境变量缺失或为空时生成随机 token；
  通过 ASP.NET Core 鉴权管线校验请求。
- 读取 Mod 目录下可选的 `Guanxiangtai.Local.json`，只接受 `mcpServer.port` 作为端口覆盖。
- 在运行目录登记自身 HTTP 入口，并在可见终端窗口打印 URL、token 来源和生成的随机 token 值。
- 持有按 Mod 目录派生的单实例锁，避免重复启动多个 server。
- 提供 `guanxiangtai_status` 只读工具，分别检测前端和后端插件是否可响应。
- 提供 `guanxiangtai_launch_taiwu`、`guanxiangtai_stop_taiwu`、`guanxiangtai_restart_taiwu` 生命周期开发工具。它们以完整太吾运行时为对象；
  启动和重启通过 Steam URI 拉起太吾并等待前后端插件可响应，停止按 `force` 或 `requestQuit` 策略请求停止并等待太吾进程消失。
- 提供 `guanxiangtai_run_csharp_script` 工具，把受信 C# 脚本请求转发到前端或后端插件。

## 外部边界

- MCP client 配置由使用者自己的 MCP client 管理。
- 默认 agent 工作区不是观象台 MCP server 的运行前提。
- Agent 的可连接入口是 MCP server HTTP endpoint；前端和后端游戏进程只由本 server 在模块内部调用，工具返回体只携带
  工具契约字段。
- 生命周期工具的参数只选择操作策略；前端/后端 side 和等待窗口由本模块按完整太吾运行时固定处理。
- token、agent 会话、脚本内容、调试会话和游戏进程 IPC 地址不写入本模块持久状态。

生命周期工具中，Steam URI 启动请求、OS 进程观察、`force` 强杀和启动/停止等待策略归本模块；`requestQuit` 的跨进程消息归
`src/Ipc/`，前端收到消息后的游戏退出动作归 `src/Frontend/`。

脚本运行中，MCP server 负责把工具调用路由到目标侧 IPC endpoint，把 `arguments` JSON 对象和 `entryThread`
写入 IPC 请求，并把内部的嵌套判别联合整理为 Agent 可读 JSON：未调用入口时返回原因和可选诊断；已调用入口时再区分入口返回值和入口异常。
实际线程切换由目标侧插件实现。

MCP server 不参与脚本编译引用解析，也不维护前后端 DLL 清单。入口契约、Mod 侧响应映射和脚本契约 DLL 引用路径归
目标插件进程内的 `src/Scripting/`；前端额外脚本引用归前端插件，后端主循环分派归
`shared/Wanxiang.Taiwu.DynamicScripting.Backend`，前端主线程分派归 `shared/Wanxiang.Taiwu.DynamicScripting.Frontend`。
通用编译、入口调用和运行事实归 shared 动态脚本运行核心。可部署依赖仍由打包项目维护。

包内运行时，server 会从 `Processes/Wanxiang.Guanxiangtai.McpServer/` 反推 Mod 目录。开发态直接运行项目时，server 输出目录作为运行目录的基准目录。
完整运行模型见 [../../docs/mcp-server-runtime.md](../../docs/mcp-server-runtime.md)。
