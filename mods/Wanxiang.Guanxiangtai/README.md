# 观象台：Mod 制作者的 MCP 入口

观象台给熟悉 agent 与 MCP 的 Mod 制作者使用，让 agent 把当前游戏运行时纳入自己的工作循环。agent 只连接观象台的
HTTP MCP server；需要游戏能力时，由 server 转给当前游戏里的前端或后端插件。

通过 MCP server，agent 可以请求游戏进程动态执行使用者信任的 C# 编译单元，按任务需要读取、判断或改动运行时状态。游戏前端会按需自动拉起
server；维护者也可以手动运行包内可执行文件。自动拉起时，常驻 server 使用独立控制台运行，不作为太吾游戏进程的常驻子进程。
游戏退出不会自动关闭 server。太吾生命周期工具和状态检测都围绕这条运行时桥梁提供。

## 适合谁

观象台面向熟悉 agent 与 MCP 使用的 Mod 制作者。

如果你只是普通游玩，不需要让开发工具访问当前游戏进程，通常不需要启用观象台。

观象台声明万象引为前置依赖，用于前端、后端插件里的跨进程通信运行时和插件子目录依赖解析。

## 启动与连接

观象台 MCP server 的可执行文件在：

```text
<MOD_DIR>\Processes\Wanxiang.Guanxiangtai.McpServer\Wanxiang.Guanxiangtai.McpServer.exe
```

手动启动不需要参数。游戏前端按需拉起 server 时，会调用同一个可执行文件的启动请求入口；该入口只负责创建独立常驻 server
进程后退出。server 使用 Streamable HTTP transport，默认监听 `127.0.0.1` 上的随机空闲端口。启动后，server 控制台窗口会打印实际 URL。

需要固定端口时，在 Mod 目录下手动创建 `Guanxiangtai.Local.json`：

```json
{
  "mcpServer": {
    "port": 37617
  }
}
```

这个文件不会被观象台打包，也不会随 Mod 默认分发。`port` 为 `0` 或文件缺失时，继续使用随机空闲端口。

MCP 请求必须使用 bearer token。需要固定 token 时，在启动常驻 server 的环境中设置：

```text
WANXIANG_GUANXIANGTAI_MCP_TOKEN
```

该值至少需要 16 个字符。server 只读取自身进程环境：手动从终端启动时，它继承当前终端环境；由游戏前端自动拉起时，
常驻 server 使用桌面用户环境，而不是太吾游戏进程的临时环境。
如果这个环境变量缺失或为空，server 会在启动时生成一串随机 token，并打印在 server 控制台窗口中。随机 token
不会被持久化，重启后会变化，也不会写入本地配置或运行态入口文件。

MCP 客户端请求 `/mcp` 时必须带：

```text
Authorization: Bearer <token>
```

## Agent 接入

标准接入方式是在 MCP client 中手动注册 HTTP server：

1. 可选：在启动常驻 server 的环境中设置 `WANXIANG_GUANXIANGTAI_MCP_TOKEN`，让 server 使用稳定 token；不设置时使用 server 控制台窗口打印的随机 token。
2. 启动游戏，或手动启动 MCP server 可执行文件。
3. 从 server 控制台窗口、固定端口配置或运行态入口文件确定 URL。
4. 在 MCP client 中注册 `http://127.0.0.1:<port>/mcp`，并让客户端使用同一个 bearer token。

MCP 客户端运行在能访问宿主本机服务的容器或其它网络命名空间内时，可以按运行环境替换 URL 里的 host；端口仍来自控制台窗口输出、显式配置或运行态入口文件。
容器内 MCP client 仍要使用同一串 token：如果 server 使用环境变量 token，把 `WANXIANG_GUANXIANGTAI_MCP_TOKEN`
传入容器内 agent；如果 server 使用随机 token，把控制台窗口打印的 token 配置给容器内 MCP client。例如 Docker Desktop 下可使用
`http://host.docker.internal:<port>/mcp`。

## MCP 工具

当前提供这些工具：

- `guanxiangtai_status`：检测 MCP server 是否能联系到观象台前端和后端插件。返回体只报告
  `frontend`、`backend` 两侧的判别联合：`available` 或 `unavailable(reason)`。MCP server 自身可用性由 MCP
  请求是否成功体现；前后端插件连接信息只写在本机运行态文件中，MCP 工具不返回这些连接信息。
- `guanxiangtai_launch_taiwu`：请求 Steam 通过 `steam://rungameid/838350` 拉起太吾绘卷，然后等待观象台前端、后端插件都可响应。
  返回体包含 `outcome`；如果固定启动等待到期，返回最后观察到的两侧可用性。
- `guanxiangtai_stop_taiwu`：开发用停止工具。`method` 只接受 `force` 或 `requestQuit`，默认 `force`。`force` 通过 OS 进程结束太吾；
  `requestQuit` 请求前端插件执行游戏退出流程。两种方式都会等待太吾前端和匹配后端进程消失；如果 `requestQuit` 请求无法被前端
  插件接受，工具返回请求失败，不进入进程消失等待。停止工具不停止 MCP server。
- `guanxiangtai_restart_taiwu`：开发用重启工具。先按 `stopMethod` 停止太吾；只有停止完成后才请求 Steam 拉起，并等待观象台前端、
  后端插件都可响应。返回体包含整体 `outcome`，默认 `stopMethod=force`。
- `guanxiangtai_run_csharp_script`：在游戏前端或后端进程内执行使用者信任的 C# 编译单元。脚本入口契约由
  [脚本执行适配模块](src/Scripting/README.md#入口契约)维护。`arguments` 是必填 JSON 对象；没有参数时传 `{}`。
  工具返回入口未调用、入口返回值或入口异常的结构化 JSON。

生命周期工具是等待式开发工具，用来减少 agent 额外轮询；工具契约只返回操作结果和两侧可用性，不提供前端/后端 side
选择或等待时长参数。
完整运行模型见 [MCP Server 运行模型](docs/mcp-server-runtime.md)。

## 运行态文件

观象台在 Mod 目录下创建 `.guanxiangtai-runtime/`，用于保存本机运行态入口信息：

- `mcp-server-endpoints.json` 记录当前 MCP server 的 HTTP 入口、实际端口和宿主侧进程路径。
- `ipc-endpoints.json` 保存 MCP server 联系前端、后端插件所需的内部 IPC 连接信息。

Agent 的稳定入口是 MCP server HTTP 地址。前端或后端游戏进程连接信息只供 MCP server 使用，不作为 agent 可定位入口。
bearer token 不写入本地配置、运行态入口文件或其它运行态文件。`ipc-endpoints.json` 不是 MCP client 配置文件，也不是
agent 可连接入口。

观象台不把运行态入口信息写入用户本地应用数据目录；退订删除 Mod 目录时，这些文件会随目录一起回收。

## 信任边界

- 观象台不设置远程服务器。
- 运行态文件记录 MCP server 入口、进程信息和内部 IPC 连接信息；token 与脚本内容不进入这些文件。
- 状态检测工具的契约是让 MCP server 确认前端、后端插件可响应；MCP server HTTP 自身健康由外层 MCP
  请求结果体现，OS 进程存活性需要通过本机进程工具判断。
- 启动、停止和重启工具是 Mod 开发辅助能力，不提供存档保护或退出确认；`force` 是默认停止策略，会直接结束匹配的游戏进程。启用前应
  只连接受信任的 MCP 客户端。生命周期工具使用固定等待窗口：启动/重启等待观象台运行时 ready 最多 5 分钟，停止等待太吾
  进程消失最多 30 秒；`requestQuit` 的前端退出请求最多等待 5 秒被接受。这些等待窗口不是工具参数；外层 MCP host 或 agent
  可以用自己的工具调用超时取消等待。
- C# 脚本工具在目标侧游戏进程内完全信任运行，不提供沙箱。启用前应只连接受信任的 MCP 客户端。
- 涉及游戏启停或调试能力时，agent 仍只连接 MCP server，由 server 转到游戏侧能力。

源码维护入口见 [DEVELOPMENT.md](DEVELOPMENT.md)。
