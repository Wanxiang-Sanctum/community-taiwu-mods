# 观象台：面向 Mod 制作者的本机 MCP 服务

观象台把一个本机 HTTP MCP server 安装成太吾 Mod。游戏前端会确保该 server 进程启动；它也可以从包内手动单独启动。
server 使用可见终端窗口运行，游戏退出不会自动关闭。

当前版本只提供 MCP server 启动、鉴权和入口定位，尚未提供可调用的游戏 tools。后续接入真实 IPC 后，agent 仍只连接 MCP server，
由 server 在内部路由到游戏侧能力。

## 适合谁

观象台面向维护太吾 Mod、准备让 AI agent 检查或操作本地游戏运行时的制作者。它不提供默认 agent 工作区，也不提供游戏内聊天人格；
MCP 客户端、项目工作区和 agent 指令由使用者自己维护。

如果你只是普通游玩，不需要让本机开发工具访问当前游戏进程，通常不需要启用观象台。

## 启动与连接

观象台 MCP server 的可执行文件在：

```text
<MOD_DIR>\Processes\Wanxiang.Guanxiangtai.McpServer\Wanxiang.Guanxiangtai.McpServer.exe
```

正常启动不需要参数。server 使用 Streamable HTTP transport，默认监听 `127.0.0.1` 上的随机空闲端口。启动后，可见终端窗口会打印实际 URL。

需要固定端口时，在 Mod 目录下手动创建 `Guanxiangtai.Local.json`：

```json
{
  "mcpServer": {
    "port": 37617
  }
}
```

这个文件不会被观象台打包，也不会随 Mod 默认分发。`port` 为 `0` 或文件缺失时，继续使用随机空闲端口。

MCP 请求必须使用 bearer token。token 只从 MCP server 进程环境变量读取，环境变量名固定为：

```text
WANXIANG_GUANXIANGTAI_MCP_TOKEN
```

该值至少需要 16 个字符。由游戏拉起 server 时，它继承游戏进程环境；手动启动时，它继承当前终端环境。

MCP 客户端请求 `/mcp` 时必须带：

```text
Authorization: Bearer <token>
```

## Agent 接入

观象台不做系统级自动注册，也不写入某个默认 agent 工作区。标准接入方式是在 MCP client 中手动注册 HTTP server：

1. 设置 `WANXIANG_GUANXIANGTAI_MCP_TOKEN`。
2. 启动游戏，或手动启动 MCP server 可执行文件。
3. 从 server 窗口、固定端口配置或运行态入口文件确定 URL。
4. 在 MCP client 中注册 `http://127.0.0.1:<port>/mcp`，并让客户端使用同一个 bearer token。

MCP 客户端运行在能访问宿主本机服务的容器或其它网络命名空间内时，只替换 host；端口仍来自窗口输出、显式配置或运行态入口文件，
token 仍来自 agent 进程自己的同名环境变量。例如 Docker Desktop 下可使用 `http://host.docker.internal:<port>/mcp`，同时把
`WANXIANG_GUANXIANGTAI_MCP_TOKEN` 传入容器内 agent。

## 运行目录

运行态入口信息写入 Mod 目录下的 `.guanxiangtai-runtime/mcp-server-endpoints.json`。该文件只记录当前 MCP server 的 HTTP 入口、
实际端口和宿主侧进程路径；前端或后端游戏进程不会作为 agent 可定位入口注册在这里。bearer token 不写入本地配置、运行态入口文件
或其它运行态文件。

观象台不把运行态入口信息写入用户本地应用数据目录；退订删除 Mod 目录时，这些文件会随目录一起回收。

## 信任边界

- 观象台是本机 MCP 服务，不设置远程服务器。
- 运行态入口文件只记录 MCP server 入口和进程信息，不记录 token、agent 会话、玩家对话或游戏进程 IPC 地址。
- 后续只有在 MCP server 接入真实 IPC 后，才会增加动态代码执行、游戏启停和调试工具；agent 仍只连接 MCP server，由 server 内部路由到游戏侧能力。这些能力不会提供沙箱，启用前应只连接受信任的本机 MCP 客户端。

源码维护入口见 [DEVELOPMENT.md](DEVELOPMENT.md)。
