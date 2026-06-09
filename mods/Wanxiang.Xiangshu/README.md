# 相枢

太吾绘卷混沌愿望回应 Mod。

## 当前阶段

当前版本验证这些运行边界：

- 前端插件和后端插件能够各自暴露本机 IPC ping endpoint。
- 前端插件能够拉起游戏外 MCP sidecar，并通过 MCP 工具区分前端侧和后端侧 IPC。
- 太吾 Mod 用户配置能够记录本机 Agent 类型、CLI 入口、工作目录和诊断显示策略。
- 进入存档后，前端诊断热键能够启动所选 CLI Agent，并要求它调用相枢工具链检查工具。

可用对话窗口、玩家对话会话、游戏状态修改和外部业务服务对接都不在当前阶段内。当前 CLI 调用只用于
诊断工具链。后续相枢内部 Agent 会话由前端插件管理；MCP server 不承载主对话协议，只作为注册给本机
Agent 的相枢工具服务。设计边界见 `docs/agent-chat.md`。

## 本机 IPC 与 MCP Sidecar

前端插件和后端插件分别启动一个仅绑定 `127.0.0.1` 的 MessagePipe TCP endpoint。两个 endpoint
都只暴露一个 ping 请求，用来验证游戏外进程能够区分连接前端侧和后端侧。

IPC endpoint 端口在插件启动时分配，并写入用户本地应用数据目录下的 manifest：

```text
Taiwu/Wanxiang.Xiangshu/ipc-endpoints.json
```

manifest 只记录发现本机 endpoint 所需的最小信息：`side`、`transport`、`host`、`path`、
`port`、`processId` 和 `startedAtUtc`。MCP server 会以 `side = "mcp-server"`、
`transport = "mcp-streamable-http"`、`path = "/mcp"` 写入同一个 manifest。

前端插件启动时会拉起 MCP sidecar。MCP server 使用 `ModelContextProtocol.AspNetCore`，由
Kestrel 直接绑定 `127.0.0.1:0`，启动后读取实际监听端口并写入同一个 manifest。插件正常释放时会
结束 MCP server；前端进程退出时，MCP server 也会停止。

当前暴露三个诊断工具：

- `xiangshu_list_endpoints`：列出 manifest 中仍存活的前端/后端 IPC endpoint。
- `xiangshu_check_toolchain`：检查当前 MCP server 是否已注册，并分别 ping 前端和后端 IPC endpoint，
  返回整条工具链是否 ready 以及每一侧的失败原因。
- `xiangshu_ping_plugin`：向 `frontend` 或 `backend` endpoint 发送 MessagePipe ping。

## 本机 Agent 配置

太吾 Mod 用户配置提供一个相枢内部 Agent 选择项、一个复用的 CLI 入口字段，以及一个工作目录字段。
切换 Codex CLI 和 Claude Code 时不需要维护两套路径字段；CLI 入口留空时会按当前选择使用默认
命令。相对工作目录会解析到相枢 Mod 目录下，默认是 `AgentWorkspace`。

调试模式只影响开发观察：开启后，相枢启动 MCP sidecar 和诊断 CLI 进程时会尽量显示控制台窗口。诊断
输出仍会结构化写入工作目录，调试模式不改变 CLI 的权限策略，也不代表已有玩家对话入口。

## 诊断热键

前端插件会把一个诊断命令注册到游戏原生热键系统的地图热键分组中。默认热键是
`Ctrl+Backslash`（`Ctrl+\`）。该命令目前在系统设置里显示为游戏内 `Mod` 文本，键位本身走游戏
原生热键保存和冲突检测。

热键只在进入存档后生效：主界面/地图界面需要正在更新，玩家已经能够操作主角，且游戏没有阻塞热键输入。
主菜单、系统设置、弹窗、剧情和其他阻塞热键的界面不作为有效测试场景。按下热键后，前端会读取当前
Agent 配置，等待相枢 MCP sidecar endpoint 注册，然后启动所选 CLI：

- Codex CLI：通过 `codex exec` 注入 `mcp_servers.xiangshu.url`，prompt 走 stdin。
- Claude Code：写入临时 `mcp-config` JSON，并用 `claude --print` 启动。

本轮诊断 prompt 会要求 Agent 调用 `xiangshu_check_toolchain`。stdout、stderr、Codex
`--output-last-message` 和退出信息会写入：

```text
<AgentWorkingDirectory>/Diagnostics/
```

触发成功时，`Player.log` 会出现 `Wanxiang.Xiangshu diagnostic hotkey accepted.` 和诊断日志目录。

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

`pack-mod` 会运行 `Taiwu.Mod.Pack.proj`，把 `Config.Lua`、前后端最终入口 DLL、声明复制的
IPC contract DLL，以及 MCP server 的 `win-x64` 发布目录组装到仓库根目录的
`artifacts/mods/Wanxiang.Xiangshu/`。前端插件从
`Processes/Wanxiang.Xiangshu.McpServer/Wanxiang.Xiangshu.McpServer.exe` 启动 MCP sidecar。IPC
contract DLL 作为独立文件部署，避免 MessagePipe 请求类型被合并改名。

插件项目的本地构建设置写在对应项目的 `Taiwu.Mod.props`。`src/Ipc/` 是前后端共用的
IPC contract、manifest 注册和本机 endpoint 辅助类库。

前端运行时依赖由 `Wanxiang.FrontendRuntime` 提供；发布后需要将该前置 mod 的 Steam Workshop
`FileId` 加入相枢的 `Dependencies`。相枢前端只部署自己的入口 DLL 和 `Wanxiang.Xiangshu.Ipc.dll`。
后端仍按自身 `net8.0` 运行时边界声明并部署 MessagePipe 相关依赖。

## 项目结构

- `Config.Lua`：游戏读取的 mod 配置。
- `Taiwu.Mod.Pack.proj`：最终可部署目录的组包声明。
- `docs/`：相枢设计说明；当前包含相枢对话体验和本机 Agent 内部设计。
- `src/Frontend/`：前端插件项目；当前启动前端 MessagePipe IPC endpoint。
- `src/Backend/`：后端插件项目；当前启动后端 MessagePipe IPC endpoint。
- `src/Ipc/`：Wanxiang.Xiangshu IPC contract、manifest 注册和本机 endpoint 辅助类库。
- `src/McpServer/`：游戏外 MCP sidecar；当前通过 MCP 工具调用前后端 IPC ping。
