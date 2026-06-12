# 相枢

太吾绘卷混沌愿望回应 Mod。

## 当前实现边界

当前版本已经形成一条最小可测的本机 Agent 对话路径：

- 前端插件和后端插件能够各自暴露本机 IPC ping endpoint。
- 前端插件能够拉起游戏外 MCP sidecar，并通过 MCP 工具区分前端侧和后端侧 IPC。
- 太吾 Mod 用户配置能够记录本机 Agent 类型、CLI 入口和工作目录。
- 进入存档后，前端热键能够打开相枢聊天窗口。
- 玩家消息能够进入前端投递会话，按轮次投递给所选 CLI Agent，并把最终答复显示为相枢消息。
- 前端能够把当前可见对话和外部 Agent 会话 id 写入 `XiangshuRuntime/ChatSessions/`，重启后在同一
  Agent 适配器下恢复当前对话窗口记录并继续复用同一个 CLI Agent 会话。

当前聊天窗口仍是运行时生成的最小界面；当前路径只覆盖本机 CLI Agent 对话。会修改游戏状态的 MCP 工具
留到后续迭代，外部业务服务对接不属于当前边界。长期上下文由本机 CLI Agent 自己的会话
系统维护；前端只保存当前可见对话和外部会话 id。MCP server 不承载主对话协议，只作为
注册给本机 Agent 的相枢工具服务。对话体验和内部协议边界见 `docs/agent-chat.md`。

## 本机 IPC 与 MCP Sidecar

前端插件和后端插件分别启动一个仅绑定 `127.0.0.1` 的 MessagePipe TCP endpoint。两个 endpoint
都只暴露一个 ping 请求，用来验证游戏外进程能够区分连接前端侧和后端侧。插件启动成功后会在游戏日志中
记录各自的监听地址、进程 ID 和 manifest 路径。

IPC endpoint 端口在插件启动时分配，并写入本机 Agent 工作目录下的 manifest：

```text
<AgentWorkingDirectory>/XiangshuRuntime/ipc-endpoints.json
```

manifest 只记录发现本机 endpoint 所需的最小信息：`side`、`transport`、`host`、`path`、
`port`、`processId` 和 `startedAtUtc`。endpoint 监听由插件生命周期维护，manifest 不承担独立心跳或
可用性协议。MCP server 会以 `side = "mcp-server"`、
`transport = "mcp-streamable-http"`、`path = "/mcp"` 写入同一个 manifest。

前端插件启动时会拉起 MCP sidecar。MCP server 是独立的 `net10.0`、`win-x64`、self-contained
裁剪发布进程，不依赖太吾后端 self-contained 运行时，也不要求玩家机器额外安装 .NET runtime。它监听
`127.0.0.1` 的随机端口，启动后把实际地址写入同一个 manifest。插件正常释放时会结束 MCP server；
前端进程退出时，MCP server 也会停止。

MCP server 使用独立进程内的 Serilog 文件日志，关键事件写入：

```text
<AgentWorkingDirectory>/XiangshuRuntime/Diagnostics/McpServer/
```

日志文件后缀为 `.events.clef`，每行是一条 compact JSON 事件。关键事件只记录启动、监听地址、
manifest 注册、父进程退出和异常，避免常驻刷屏。

当前暴露三个诊断工具：

- `xiangshu_list_endpoints`：列出 manifest 中当前可发现的前端/后端 IPC endpoint。
- `xiangshu_check_toolchain`：检查当前 MCP server 是否已注册，并用 ping 诊断当前前端和后端 IPC 链路。
- `xiangshu_ping_plugin`：向 `frontend` 或 `backend` endpoint 发送 MessagePipe ping。

当前还暴露一个对话体验工具：

- `xiangshu_send_intermediate_reply`：向当前本地聊天会话发送一条相枢中间答复。工具参数只包含要显示给
  玩家看的文本。

## 本机 Agent 配置

太吾 Mod 用户配置提供一个相枢内部 Agent 选择项、一个复用的 CLI 入口字段，以及一个工作目录字段。
切换 Codex CLI 和 Claude Code 时不需要维护两套路径字段；CLI 入口留空时会按当前选择使用默认
命令。相对工作目录会解析到相枢 Mod 目录下，默认是 `AgentWorkspace`。

这些配置是运行时启动参数。前端插件和后端插件只在初始化时读取一次；在游戏内修改设置后，需要重启游戏
来重建 IPC endpoint、manifest 路径、MCP sidecar 和本机 Agent 会话。运行中的对话继续使用本次启动时
加载的工作目录和 CLI 适配器。

默认包内会预置 `AgentWorkspace/AGENTS.md`，作为相枢的默认本机 Agent 工作区配置和自定义示范；
`AgentWorkspace/CLAUDE.md` 只负责让 Claude Code 转向同目录的 `AGENTS.md`。用户可以在这个工作区
维护自己的人设、指令、设置和 Agent 技能；配置到其它工作目录时，该目录由用户自行维护。

相枢运行时代码把每轮对话序列化为结构化回合输入，字段包括参与者和
本轮玩家消息；玩家参与者名来自当前太吾角色真实姓名。前端捕获 CLI 返回的外部会话 id，
并在后续轮次中恢复同一个本机 Agent 会话。

`XiangshuRuntime/` 是相枢 Mod 的运行数据目录，当前保存 IPC manifest、MCP 诊断日志、前端启动本机
Agent 时使用的短生命周期协议文件，以及当前聊天会话文件；游戏内当前只有聊天入口，没有会话管理界面。

聊天调用和工具链诊断都使用本次启动时加载的配置。聊天调用会等待相枢 MCP sidecar endpoint 注册，然后
把当前对话轮次交给所选 CLI Agent。前端默认按完全信任式非交互模式启动 CLI，以避免游戏内对话等待
权限交互；`AgentWorkingDirectory` 因此应视为本机 Agent 的受信工作区。

## 日志边界

游戏进程内的运行信息只进入太吾游戏日志系统；相枢前端和后端插件不另建持久化日志文件。聊天 CLI 调用的
标准输出和标准错误由前端在内存中捕获，只有摘要和错误进入游戏日志。Codex `--output-last-message`、
Codex `--output-schema` 和 Claude `--mcp-config` 使用 `XiangshuRuntime/Temp/AgentCli/` 下的临时协议
文件，调用结束后删除对应调用子目录。

前端插件和后端插件统一通过 `shared/Wanxiang.Taiwu.Logging` 记录结构化上下文。共享库把上下文序列化为
单行紧凑 JSON，并交给游戏日志系统；它不接管 MCP server 的独立进程日志。MCP server 是游戏外独立
进程，保留自己的事件日志目录作为开发观察入口。

## 聊天热键

前端插件会把一个相枢聊天命令注册到游戏原生热键系统的地图热键分组中。默认热键是
`Ctrl+Backslash`（`Ctrl+\`）。该命令目前在系统设置里显示为游戏内 `Mod` 文本，键位本身走游戏
原生热键保存和冲突检测。

热键只在进入存档后生效：主界面/地图界面需要正在更新，玩家已经能够操作主角，且游戏没有阻塞热键输入。
主菜单、系统设置、弹窗、剧情和其他阻塞热键的界面不作为有效测试场景。按下热键后，前端会打开或关闭
相枢聊天窗口；窗口打开时同一热键仍可用于关闭。

玩家送出消息后，前端会使用启动时加载的 Agent 配置，等待相枢 MCP sidecar endpoint 注册，然后启动所选
CLI：

- Codex CLI：通过 `codex exec` 注入 `mcp_servers.xiangshu.url`，结构化回合输入走 stdin，并用
  `--output-schema` 约束最终回复。
- Claude Code：写入临时 `mcp-config` JSON，用 `claude --print` 启动，并用 `--json-schema`
  约束最终回复。

结构化回合输入会带入本轮玩家消息和当前太吾角色真实姓名；历史上下文由恢复后的本机 Agent 会话提供。
最终回复必须是包含 `reply` 字段的 JSON；前端只提取 `reply` 显示给玩家。触发成功时，
游戏日志会出现相枢 `chat hotkey accepted` 记录。CLI 启动、MCP 注册、调用失败或回复解析失败时，
玩家界面显示一条相枢固定说明，详细错误通过游戏日志记录。

## 开发

从仓库根目录构建插件项目：

```powershell
dotnet build mods/Wanxiang.Xiangshu/src/Frontend/Wanxiang.Xiangshu.Frontend.csproj
dotnet build mods/Wanxiang.Xiangshu/src/Backend/Wanxiang.Xiangshu.Backend.csproj
dotnet build mods/Wanxiang.Xiangshu/src/McpServer/Wanxiang.Xiangshu.McpServer.csproj
```

打包可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name Wanxiang.Xiangshu
```

`pack-mod` 会运行 `Taiwu.Mod.Pack.proj`，把 `Config.Lua`、`AgentWorkspace/`、前后端最终入口 DLL、
声明复制的 IPC contract DLL，以及 MCP server 的 `net10.0/win-x64` self-contained 裁剪发布目录组装到
仓库根目录的 `artifacts/mods/Wanxiang.Xiangshu/`。前端插件从
`Processes/Wanxiang.Xiangshu.McpServer/Wanxiang.Xiangshu.McpServer.exe` 启动 MCP sidecar。IPC
contract DLL 作为独立文件部署，避免 MessagePipe 请求类型被合并改名。

插件项目的本地构建设置写在对应项目的 `Taiwu.Mod.props`。`src/Ipc/` 是前后端共用的
IPC contract、manifest 注册和本机 endpoint 辅助类库。

前端运行时依赖由 `Wanxiang.FrontendRuntime` 提供；发布后需要将该前置 mod 的 Steam Workshop
`FileId` 加入相枢的 `Dependencies`。相枢前端只部署自己的入口 DLL 和 `Wanxiang.Xiangshu.Ipc.dll`。
后端仍按自身 `net8.0` 运行时边界声明并部署 MessagePipe 相关依赖。

## 项目结构

- `Config.Lua`：游戏读取的 mod 配置。
- `AgentWorkspace/`：默认本机 Agent 工作区；打包后作为 `AgentWorkingDirectory` 的默认目录，包含
  `AGENTS.md`、Claude Code 兼容用的 `CLAUDE.md`，以及相枢 Mod 的运行数据目录 `XiangshuRuntime/`。
- `Taiwu.Mod.Pack.proj`：最终可部署目录的组包声明。
- `docs/`：相枢设计说明；当前包含相枢对话体验和本机 Agent 内部设计。
- `src/Frontend/`：前端插件项目；根目录保留插件生命周期组合根，`Agent/`、`Chat/`、`HotKeys/`、
  `Ipc/` 和 `Sidecar/` 分别承载本机 Agent 调用、聊天会话与窗口、热键、前端 IPC 和 MCP sidecar
  进程生命周期。
- `src/Backend/`：后端插件项目；当前启动后端 MessagePipe IPC endpoint。
- `src/Ipc/`：Wanxiang.Xiangshu IPC contract、manifest 注册和本机 endpoint 辅助类库。
- `src/McpServer/`：游戏外 MCP sidecar；当前通过 MCP 工具调用前后端 IPC ping。
