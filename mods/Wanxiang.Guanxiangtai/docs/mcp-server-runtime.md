# MCP Server 运行模型

观象台的外部入口只有 MCP server 的 HTTP `/mcp`。agent 不连接前端或后端游戏进程；工具需要游戏能力时，由 MCP server
在模块内部路由到游戏侧插件。

运行态入口文件 `.guanxiangtai-runtime/mcp-server-endpoints.json` 只服务同一 Mod 内的前端启动器和 MCP server。它不是 MCP
协议发现文件，不是生成物清单，也不是给后端插件发布游戏进程能力的契约。

## 进程关系

运行模型包含四类参与者：

- Agent/MCP client：只连接 MCP server 的 HTTP `/mcp` 入口。
- 前端插件：随游戏前端加载，负责确保 MCP server 进程存在，并发布前端 IPC endpoint。
- 后端插件：随游戏后端加载，发布后端 IPC endpoint。
- MCP server：游戏外常驻进程，负责 HTTP MCP transport、鉴权、自身入口登记和 MCP 工具。

`src/McpServerRuntime/` 只被 `src/Frontend/` 和 `src/McpServer/` 引用。后端插件不是这个模块的调用方；
关系保持为 `agent -> MCP server -> 前端或后端插件`。后端插件不发布 agent 可连接的入口，也不读取 MCP server
外部入口文件；前端插件引用 `src/McpServerRuntime/` 只为判断是否需要拉起 MCP server。

## 启动与配置

MCP server 的包内相对路径固定为：

```text
Processes/Wanxiang.Guanxiangtai.McpServer/Wanxiang.Guanxiangtai.McpServer.exe
```

server 正常启动不需要参数。游戏前端插件会确保它启动；也可以手动启动这个可执行文件。进程使用可见终端窗口，且不随游戏进程退出。

server 使用 Streamable HTTP transport，绑定宿主侧 loopback 地址。默认端口为随机空闲端口；Mod 目录下可选的
`Guanxiangtai.Local.json` 可以固定端口：

```json
{
  "mcpServer": {
    "port": 37617
  }
}
```

`port` 取值范围是 `0` 到 `65535`。`0` 表示随机空闲端口。这个文件由使用者手工创建，不进入发布包，也不是远程暴露开关。

bearer token 优先从进程环境变量 `WANXIANG_GUANXIANGTAI_MCP_TOKEN` 读取；显式设置时至少需要 16 个字符。环境变量缺失或为空时，
server 会生成随机 token，并只打印在可见终端窗口中。token 不从本地配置覆盖，不写入运行态文件。server 通过 ASP.NET Core
authentication/authorization 管线保护 `/mcp`；有效 MCP 请求必须带 `Authorization: Bearer <token>`。

控制台输出只承担启动者连接提示和配置错误提示，例如 URL、token 来源和生成的随机 token。成功 MCP 请求、轮询和心跳不作为
控制台提示；框架请求日志过滤到 `Warning` 以上，避免每次请求打印一条日志。

## 运行目录

包内运行时，运行目录位于 Mod 目录下：

```text
.guanxiangtai-runtime/
```

开发态直接运行 MCP server 项目时，运行目录位于 server 输出目录下的 `.guanxiangtai-runtime/`。运行目录不落在 agent 工作区，
也不通过启动参数覆盖。观象台不把这类运行态入口信息写入用户本地应用数据目录。

## 运行态入口文件

运行态入口文件路径为：

```text
.guanxiangtai-runtime/mcp-server-endpoints.json
```

文件由 MCP server 写入，由前端启动器和同一 MCP server 读取。当前不设置独立版本字段；时间字段使用带偏移的 ISO 8601 值，
字段名只表达事件含义，不额外写 `Utc`。

当前形态：

```json
{
  "modId": "Wanxiang.Guanxiangtai",
  "updatedAt": "2026-06-29T00:00:00+00:00",
  "servers": [
    {
      "host": "127.0.0.1",
      "path": "/mcp",
      "port": 54321,
      "processId": 1234,
      "startedAt": "2026-06-29T00:00:00+00:00",
      "executablePath": "...\\Wanxiang.Guanxiangtai.McpServer.exe"
    }
  ]
}
```

`servers` 只描述 agent 应连接的 MCP server。前端和后端游戏进程不会写入这里。MessagePipe、调试协议或其它本机连接方式属于
MCP server 内部路由和游戏插件内部协议，不改变这个外部入口模型。

运行态入口文件中的 `host` 是 server 在宿主侧实际绑定的本机地址。容器、WSL 或其它本地隔离环境里的 MCP client 可以按自己的运行环境替换
host，但不能把替换结果写回文件；运行态入口文件仍由宿主侧进程维护。

## 单实例

MCP server 启动后持有按 Mod 目录派生的命名 mutex。已有 live server 时，新启动的 server 会输出已有 HTTP 入口并退出。
前端插件启动 server 前也会先读取运行态入口文件，避免正常路径下重复打开终端窗口。

`McpServerEndpointRegistry` 判断 live server 时会确认记录中的 PID 仍存活，并要求 `executablePath` 与当前 PID 的进程映像路径一致，避免 PID
复用把陈旧记录误判为活跃。
运行态入口文件的并发读写使用按文件路径派生的命名 mutex，不在运行目录里创建额外锁文件。

## Agent 注册

观象台的标准接入路径是手动 HTTP MCP 注册；面向使用者的步骤见 [README.md](../README.md#agent-接入)。本设计只固定边界：

- 观象台不做系统级自动注册，也不维护 MCP client 配置。
- MCP client 只需要 HTTP URL 和 bearer token；配置文件位置和字段名由具体客户端决定。
- 固定端口来自 `Guanxiangtai.Local.json`，随机端口来自可见窗口输出或 `.guanxiangtai-runtime/mcp-server-endpoints.json`。
- 容器内 agent 不能自动读取宿主环境变量；需要把同一 token 值传入或配置给容器内 MCP client，并把 URL 的 host
  投影为容器能访问宿主服务的地址。

## 内部 IPC 与工具

观象台的前端和后端插件会把内部 MessagePipe endpoint 登记到：

```text
.guanxiangtai-runtime/ipc-endpoints.json
```

这个文件只服务 MCP server 内部路由，不是 MCP client 配置文件，也不是 agent 可连接入口。当前 MCP 工具分为状态、生命周期和脚本三类：

- `guanxiangtai_status`：MCP server 分别向前端和后端 endpoint 发送状态请求；每侧返回 `kind` 判别的结构化结果：
  `available` 或 `unavailable(reason)`。
- `guanxiangtai_launch_taiwu`：MCP server 请求 Steam 打开 `steam://rungameid/838350`，随后等待观象台前端、后端 IPC 都可响应。
  `launch.kind=launchRequested` 只表示 Steam URI 已交给宿主 shell；工具返回体的整体 `outcome` 和 `runtimeReady` 才描述观象台
  运行时是否在内部等待窗口内 ready。如果内部启动等待到期，返回最后观察到的两侧可用性。
- `guanxiangtai_stop_taiwu`：MCP server 按 `method` 停止太吾，默认 `force`。`force` 是 OS 级强杀：先杀本机太吾前端进程树，
  再补杀路径匹配 `Backend\GameData.exe` 的遗留后端进程；`requestQuit` 是前端 IPC 请求：让前端插件在 Unity 主线程设置
  `GameApp.ReadyToQuit` 并调用 `GameApp.QuitGame()`。两种方式都会等待太吾前端和匹配后端进程消失；如果 `requestQuit` 请求无法
  被前端 IPC 接受，工具返回 `kind=requestQuitFailed`，不进入进程消失等待。
- `guanxiangtai_restart_taiwu`：MCP server 先按 `stopMethod` 停止太吾；停止完成后才请求 Steam 拉起太吾，并等待观象台前端、后端
  IPC 都可响应。返回体包含整体 `outcome`；如果停止没有完成，工具不会继续拉起。
- `guanxiangtai_run_csharp_script`：MCP server 按 `targetSide` 选择前端或后端 endpoint，发送受信 C# 脚本执行请求，并把
  `arguments` JSON 对象和 `entryThread` 转发给目标侧。

状态工具不报告 MCP server 自身可用性；能返回工具结果已经说明 MCP transport、鉴权和工具调用链路可用。状态工具也不报告
OS 进程存活性，避免把 manifest 或 PID 观察误包装成游戏侧可用事实。

生命周期工具不把太吾前后端建模成可独立启动或停止的 side；太吾后端由前端启动，并接收前端传入的 Mod 列表和运行配置。因此
`guanxiangtai_launch_taiwu` 和 `guanxiangtai_restart_taiwu` 的完成条件是观象台前端、后端 IPC 都可响应，而不是某一侧单独可用。

生命周期工具使用固定的内部等待窗口，工具参数保持为操作策略，不包含 `timeoutSeconds` 一类超时字段：启动/重启等待观象台运行时 ready
最多 5 分钟，停止等待太吾进程消失最多 30 秒，`requestQuit` 的前端 IPC 请求最多等待 5 秒被接受。外层 MCP host 或 agent
仍可用自己的工具调用超时取消等待。

运行态入口文件和 IPC manifest 不保存 token、权限决策、脚本内容、调试会话、agent 消息、玩家可见文本或游戏进程 IPC 地址。需要传递或
持久化这些内容时，由拥有对应语义的 IPC 协议、工具协议或运行数据文件维护。
