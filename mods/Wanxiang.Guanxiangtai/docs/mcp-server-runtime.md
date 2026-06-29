# MCP Server 运行模型

观象台的外部入口只有 MCP server 的 HTTP `/mcp`。agent 不连接前端或后端游戏进程；后续 tools 需要游戏能力时，也应由 MCP server
通过内部 IPC/bridge 路由。

运行态入口文件 `.guanxiangtai-runtime/mcp-server-endpoints.json` 只服务同一 Mod 内的前端启动器和 MCP server。它不是 MCP
协议发现文件，不是生成物清单，也不是给后端插件发布游戏进程能力的契约。

## 进程关系

当前实现只有三类参与者：

- Agent/MCP client：只连接 MCP server 的 HTTP `/mcp` 入口。
- 前端插件：随游戏加载，负责确保 MCP server 进程存在；它可以读取运行态入口文件来避免重复拉起。
- MCP server：游戏外常驻进程，负责 HTTP MCP transport、鉴权和自身入口登记。

`src/McpServerRuntime/` 只被 `src/Frontend/` 和 `src/McpServer/` 引用。后端插件不是这个模块的调用方；后续增加后端插件时，
关系仍应保持为 `agent -> MCP server -> 内部 IPC/bridge -> 后端插件`。后端插件不发布 agent 可连接的入口，也不读取运行态入口文件。

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

bearer token 只从进程环境变量 `WANXIANG_GUANXIANGTAI_MCP_TOKEN` 读取，不从本地配置覆盖，不写入运行态文件。server 通过 ASP.NET
Core authentication/authorization 管线保护 `/mcp`；有效 MCP 请求必须带 `Authorization: Bearer <token>`。

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

`servers` 只描述 agent 应连接的 MCP server。前端和后端游戏进程不会写入这里。后续接入 MessagePipe、调试协议或其它本机连接方式时，
那些连接属于 MCP server 内部路由和游戏插件内部协议，不改变这个外部入口模型。

运行态入口文件中的 `host` 是 server 在宿主侧实际绑定的本机地址。容器、WSL 或其它本地隔离环境里的 MCP client 可以按自己的运行环境替换
host，但不能把替换结果写回文件；运行态入口文件仍由宿主侧进程维护。

## 单实例

MCP server 启动后持有按 Mod 目录派生的命名 mutex。已有 live server 时，新启动的 server 会输出已有 HTTP 入口并退出。
前端插件启动 server 前也会先读取运行态入口文件，避免正常路径下重复打开终端窗口。

`McpServerEndpointRegistry` 判断 live server 时会确认记录中的 PID 仍存活，并要求 `executablePath` 与当前 PID 的进程映像路径一致，避免 PID
复用把陈旧记录误判为活跃。
运行态入口文件的并发读写使用按文件路径派生的命名 mutex，不在运行目录里创建额外锁文件。

## Agent 注册

观象台当前提供的标准接入路径是手动 HTTP MCP 注册，不做系统级自动注册，也不维护 MCP client 配置：

1. Mod 制作者设置 `WANXIANG_GUANXIANGTAI_MCP_TOKEN`。
2. 确保 MCP server 进程能读到该环境变量。由游戏前端插件拉起时，它继承游戏进程环境；手动启动时，它继承当前终端环境。
3. 启动游戏或手动启动 `Processes/Wanxiang.Guanxiangtai.McpServer/Wanxiang.Guanxiangtai.McpServer.exe`。
4. 确定 URL：固定端口来自 `Guanxiangtai.Local.json`；随机端口来自可见窗口输出或 `.guanxiangtai-runtime/mcp-server-endpoints.json`。
5. 在 MCP client 配置里注册 HTTP server，URL 使用上一步端口，鉴权使用同一个 bearer token。

支持 HTTP MCP 的客户端通常需要注册两项信息：URL 为 `http://127.0.0.1:<port>/mcp`，bearer token 来自
`WANXIANG_GUANXIANGTAI_MCP_TOKEN`。配置文件位置和字段名由具体客户端决定。

容器内 agent 不能自动读取宿主环境变量；需要把同名环境变量传入容器，并把 URL 的 host 投影为容器能访问宿主服务的地址。

## 后续 IPC 与 Tools

当前版本的外部接入止于 MCP client 成功连接，还没有可调用的游戏内 IPC。后续接入真实 IPC 后，agent 仍只连接 MCP server；
MCP server 再按 tool 语义选择前端或后端游戏进程执行请求。

运行态入口文件不保存权限决策、脚本内容、调试会话、agent 消息、玩家可见文本或游戏进程 IPC 地址。那些内容应由 MCP server 内部 IPC 协议、
tool 协议和运行数据目录的专属文件维护。
