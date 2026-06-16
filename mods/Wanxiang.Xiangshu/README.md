# 相枢

太吾绘卷本机 Agent 对话 Mod。

相枢在游戏内提供与“相枢”的对话入口，并通过本机 MCP sidecar 让 Agent 调用相枢提供的工具能力。工具边界
重点是前后端受信 C# 脚本执行，以及同一轮对话中的中间答复。

## 能力边界

相枢由三条运行链路组成：

- 游戏内对话：前端插件注册聊天热键，显示玩家与相枢的可见消息，并把玩家输入投递给所选本机 CLI Agent。
- 本机工具服务：前端拉起 MCP sidecar；sidecar 通过本机 IPC 把工具请求路由到前端或后端插件进程。
- 会话衔接：前端保存游戏内可见对话和外部 Agent 会话 id；长期上下文仍由本机 CLI Agent 自己维护。

脚本工具以完全信任方式在目标插件进程内运行，适合受信工作区和开发调试场景。相枢不提供远程服务，也不把
脚本能力包装成面向非受信输入的沙箱。

对话、运行目录、脚本通道和 CLI 适配器的内部设计见 `docs/agent-chat.md`；日志策略见 `docs/logging.md`。

## 游戏内使用

默认热键是 `Ctrl+Backslash`（`Ctrl+\`）。进入存档并回到可操作的主界面/地图交互后，按下热键可打开或
关闭相枢聊天窗口。

玩家发送消息后，前端会使用启动时加载的本机 Agent 配置投递一个对话轮次。Agent 工作期间，发送入口会
切换为“且慢”；玩家点击后，当前回应会立刻中断，聊天流追加“且慢”。这条“且慢”不会单独交给 Agent，
而会在玩家下一次发送普通消息时一起进入新的投递。

聊天窗口头部提供重置入口。重置会清空当前游戏内可见对话，并新建本地投递会话。

## 本机 Agent 配置

太吾 Mod 用户配置提供三个字段：

- `AgentAdapter`：选择 `Codex CLI` 或 `Claude Code`。
- `AgentCliPath`：本机 Agent CLI 的命令名或可执行文件路径；留空时使用当前 Agent 的默认命令。
- `AgentWorkingDirectory`：本机 Agent 的工作目录；相对路径会解析到相枢 Mod 目录下，默认是
  `AgentWorkspace`。

本地进阶设置不放进太吾 Mod 配置界面，也不属于 Agent 工作区。如果需要给 CLI Agent 传环境变量，在相枢
Mod 目录下创建与 `Config.Lua` 同级的 `LocalSettings.json`：

```json
{
  "agent": {
    "env": {
      "HTTPS_PROXY": "http://127.0.0.1:7890"
    }
  }
}
```

相枢启动 Codex/Claude 子进程时，会把 `agent.env` 中的字符串键值写入子进程环境。这些变量不传给游戏
进程或 MCP sidecar，也不会写入诊断日志。

太吾 Mod 用户配置和 `LocalSettings.json` 都在插件初始化时读取；修改后需要重启游戏来重建 IPC endpoint、
MCP sidecar、运行数据目录、本机 Agent 会话和 CLI 子进程环境。

默认包内预置 `AgentWorkspace/`，作为可编辑的本机 Agent 工作区示例。配置到其它工作目录时，该目录由用户
自行维护。

## 运行数据与诊断

相枢把运行数据写入 `AgentWorkingDirectory/.xiangshu-runtime/`。这个目录由相枢运行时维护，用于 endpoint
发现、聊天会话恢复、临时 CLI 协议文件和 MCP sidecar 事件日志。用户维护工作区时，应把它视为运行数据
目录，而不是可编辑 Agent 资产或对话记录。

游戏内可见对话不写入诊断日志。前端和后端插件只把启动边界、可恢复边界问题和失败原因写入太吾游戏日志；
MCP sidecar 是独立进程，它的生命周期事件写入 `.xiangshu-runtime/Diagnostics/McpServer/`。事件选择、
上下文字段和运行数据边界见 `docs/logging.md`。

## 开发

从仓库根目录构建主要项目：

```powershell
dotnet build mods/Wanxiang.Xiangshu/src/Frontend/Wanxiang.Xiangshu.Frontend.csproj
dotnet build mods/Wanxiang.Xiangshu/src/Backend/Wanxiang.Xiangshu.Backend.csproj
dotnet build mods/Wanxiang.Xiangshu/src/McpServer/Wanxiang.Xiangshu.McpServer.csproj
```

打包可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name Wanxiang.Xiangshu
```

`pack-mod` 会运行 `Taiwu.Mod.Pack.proj`，把 `Config.Lua`、`AgentWorkspace/`、前后端入口 DLL，以及 MCP
sidecar 的发布目录组装到仓库根目录的 `artifacts/mods/Wanxiang.Xiangshu/`。前端插件从
`Processes/Wanxiang.Xiangshu.McpServer/Wanxiang.Xiangshu.McpServer.exe` 启动 sidecar。

相枢依赖 `Wanxiang.Prelude`（万象引）提供共享运行时和按入口目录优先解析 DLL 的加载规则。发布时，将
万象引的 Steam Workshop `FileId` 加入相枢的 `Dependencies`，确保万象引先于相枢加载。具体运行时清单由
万象引和相枢各自的项目文件维护，根 README 不复制 DLL 清单。脚本编译和程序集解析的模块边界见
`src/Scripting/README.md`。

## 项目结构

根 README 说明入口和所有权；对话运行设计见 `docs/agent-chat.md`，日志策略见 `docs/logging.md`。源码模块
增长约定优先看对应目录下的 `README.md`。

- `Config.Lua`：游戏读取的 Mod 配置。
- `AgentWorkspace/`：默认本机 Agent 工作区示例，包含入口上下文、轻量静态语境和对应 CLI Agent 可发现的技能
  目录。
- `Taiwu.Mod.Pack.proj`：最终可部署目录的组包声明。
- `docs/`：对话链路和日志策略等内部设计说明。
- `src/Frontend/`：前端插件项目，负责游戏内对话入口、本机 Agent 投递、前端 IPC 和 sidecar 生命周期。
- `src/Backend/`：后端插件项目，负责后端 IPC 和后端侧脚本执行入口。
- `src/Ipc/`：前端、后端和 MCP sidecar 共享的 contract 与 endpoint 辅助库。
- `src/Scripting/`：前后端复用的受信 C# 脚本编译与执行器。
- `src/McpServer/`：游戏外 MCP sidecar。
