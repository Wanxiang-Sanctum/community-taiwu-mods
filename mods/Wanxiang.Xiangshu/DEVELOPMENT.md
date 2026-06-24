# 相枢 Mod 维护入口

本文面向维护相枢 Mod 源码、默认 Agent 工作区和发布内容的人。玩家使用、配置和信任边界见 [README.md](README.md)。

## 内部设计入口

[`docs/README.md`](docs/README.md) 是源码维护者的内部设计文档入口，指向对话链路、日志策略和默认 Agent 工作区来源说明。
`DefaultAgentWorkspace/AGENTS.md` 是组包后运行中的本地 Agent 指令入口，承担基础相枢身份、口吻、玩家可见边界和输出
可见性；其中的 `persona/`、`lore/`、`tool-guides/` 和技能目录应保持自包含，不依赖源码维护文档。

源码模块边界优先看对应 `src/*/README.md`。

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

`pack-mod` 会运行 `Taiwu.Mod.Pack.proj`，把 `Config.Lua`、`DefaultAgentWorkspace/`、前后端入口 DLL，以及 MCP sidecar
的发布目录组装到仓库根目录的 `artifacts/mods/Wanxiang.Xiangshu/`。前端插件从
`Processes/Wanxiang.Xiangshu.McpServer/Wanxiang.Xiangshu.McpServer.exe` 启动 sidecar。

相枢 Mod 依赖 `Wanxiang.Prelude`（万象引）提供共享运行时和按入口目录优先解析 DLL 的加载规则。发布时，将万象引的 Steam
Workshop `FileId` 加入相枢 Mod 的 `Dependencies`，确保万象引先于相枢 Mod 加载。具体运行时清单由万象引和相枢 Mod 各自的项目文件
维护；共享脚本编译规则见 `src/Scripting/README.md`，前端宿主适配边界见 `src/Frontend/README.md`。

## 默认 Agent 工作区维护

源码仓库中默认 Agent 工作区资料的来源和维护边界见 `docs/agent-context-sources.md`。发布后的 `DefaultAgentWorkspace/`
以包内文件作为完整 Agent 工作区；源码维护时以太吾游戏文本、配置、游戏侧源码和运行时事实为资料来源，组织内部维护者可使用
[`taiwu-modkit`](https://github.com/Wanxiang-Sanctum/taiwu-modkit) 仓库根目录下的 `game/` 快照核对这些游戏侧资料。

`DefaultAgentWorkspace/` 是发布内容源。运行中产生的 `.xiangshu-notes/` 和 `.xiangshu-runtime/` 不应作为默认 Agent 工作区资产
提交；只有已经稳定到应随默认 Agent 工作区发布的内容，才按 `docs/agent-context-sources.md` 的放置规则改写进稳定资产。

## 项目结构

- `Config.Lua`：游戏读取的 Mod 配置。
- `DefaultAgentWorkspace/`：默认本地 Agent 工作区内容；其中 `AGENTS.md` 放基础相枢身份、口吻、玩家可见边界和读取路由，
  `CLAUDE.md` 转发到同一入口，`persona/` 放更细的人设校准，`lore/` 放按需读取的世界观资料，`tool-guides/` 放玩家
  视图观察、脚本执行边界、事件、经历、GM 调试入口、领域上下文和游戏知识检索指引，并保留对应 CLI Agent 可发现的技能目录。
- `Taiwu.Mod.Pack.proj`：最终可部署目录的组包声明。
- `docs/`：对话链路、CLI 适配器、日志策略和默认 Agent 工作区来源等内部设计说明；入口见 `docs/README.md`。
- `src/Frontend/`：前端插件项目，负责游戏内对话入口、行囊寄身物风味及其非持久运行态、本地 Agent 投递、前端 IPC 和
  sidecar 生命周期。
- `src/Backend/`：后端插件项目，负责供 MCP sidecar 调用的后端 IPC、后端侧脚本执行入口，以及安装 shared 行囊宿主
  观察服务。
- `src/Ipc/`：MCP sidecar 调用前端、后端插件时共享的 MessagePipe 请求/响应契约与 endpoint 辅助库。
- `src/Scripting/`：前后端复用的受信 C# 脚本编译与执行器。
- `src/McpServer/`：游戏外 MCP sidecar。
