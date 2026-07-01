# 观象台 Mod 维护入口

本文面向维护观象台源码、组包内容和发布流程的人。使用说明见 [README.md](README.md)。

内部设计入口见 [docs/README.md](docs/README.md)。源码模块边界优先看对应 `src/*/README.md`，不要在本文件复制模块细节。

## 常用命令

从仓库根目录构建插件项目：

```powershell
dotnet build mods/Wanxiang.Guanxiangtai/src/Frontend/Wanxiang.Guanxiangtai.Frontend.csproj
dotnet build mods/Wanxiang.Guanxiangtai/src/Backend/Wanxiang.Guanxiangtai.Backend.csproj
dotnet build mods/Wanxiang.Guanxiangtai/src/McpServer/Wanxiang.Guanxiangtai.McpServer.csproj
```

打包可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name Wanxiang.Guanxiangtai
```

`pack-mod` 会运行 `Taiwu.Mod.Pack.proj`，把它声明的内容组装到仓库根目录的 `artifacts/mods/Wanxiang.Guanxiangtai/`。
当前包内容包括 `Config.Lua`、前端和后端插件入口，以及 `Processes/Wanxiang.Guanxiangtai.McpServer/` 下的 MCP server 发布目录。

## 组包边界

最终包内容由 `Taiwu.Mod.Pack.proj` 声明。新增静态文件、目录、额外项目或发布目录时，先按上一级
[mods/README.md](../README.md) 选择组包 item，再更新组包入口或对应项目文件。

插件项目的本地构建设置写在对应项目的 `Taiwu.Mod.props`。通过 `ProjectReference` 引用的 `shared/` 项目会由 `pack-mod`
自动合并，不在 `Taiwu.Mod.props` 中声明合并或复制动作；非 shared 依赖需要合并或复制时，再通过
`TaiwuModMergeDependency` 和 `TaiwuModCopyDependency` 声明。普通 `dotnet build` 保持项目常规输出。

## 项目结构

- `Config.Lua`：游戏读取的 Mod 配置；字段语义见仓库级文档
  [太吾游戏 Mod 配置与 Steam 发布边界](../../docs/taiwu-mod-steam-publishing-boundary.md)。当前不公开 Mod 设置。观象台声明
  万象引为前置依赖，承接游戏进程内 MessagePipe 运行时和插件子目录依赖解析。
- `Guanxiangtai.Local.json`：用户手工放在 Mod 目录下的本地端口配置文件；组包入口不声明它。
- `Taiwu.Mod.Pack.proj`：最终可部署目录的组包声明。
- `docs/`：内部设计说明，入口见 `docs/README.md`。
- `src/McpServerRuntime/`：前端启动器和 MCP server 本体共享的运行态协调模块，维护 MCP server 外部入口文件。
- `src/Ipc/`：MCP server 到前端、后端插件的内部 MessagePipe IPC 契约、endpoint manifest、状态检测、游戏退出和脚本执行消息。
- `src/Scripting.Contracts/`：观象台脚本可见的窄契约程序集，承载脚本入口参数等稳定脚本契约。
- `src/Scripting/`：观象台脚本入口适配层，声明入口类型约定、运行器适配和响应映射，并复用 shared 动态脚本运行核心。
- `src/Frontend/`：前端插件项目，入口 DLL 部署到 `Plugins/Frontend/`，负责确保 MCP server 进程启动、发布前端 IPC endpoint，
  并承接前端侧游戏退出和脚本执行。
- `src/Backend/`：后端插件项目，入口 DLL 部署到 `Plugins/Backend/`，负责发布后端 IPC endpoint，并承接后端侧脚本执行。
- `src/McpServer/`：游戏外 MCP Streamable HTTP server，负责常驻 HTTP 入口、鉴权、自身入口注册、太吾生命周期、状态检测和脚本执行工具。

## 同步清单

- 公开配置或面向使用者的说明变化时，同步更新 [README.md](README.md)。
- 包内容变化时，同步更新 `Taiwu.Mod.Pack.proj`、项目文件或项目旁 `Taiwu.Mod.props` 中拥有该声明的位置。
- 非 shared 依赖合并、复制或 Publicizer 设置变化时，同步更新对应项目的 `Taiwu.Mod.props`。
- 运行态入口文件字段、运行目录解析、MCP server 启动方式、连接鉴权、token 来源、可见控制台提示或请求日志过滤变化时，同步更新
  [docs/mcp-server-runtime.md](docs/mcp-server-runtime.md) 和对应拥有该边界的模块 README；涉及运行态协调常量或入口登记时，同步更新
  `src/McpServerRuntime/README.md`。
- 内部 IPC 契约、endpoint manifest、生命周期工具、状态工具或脚本工具语义变化时，同步更新 `src/Ipc/README.md`、`src/McpServer/README.md`
  和 [docs/mcp-server-runtime.md](docs/mcp-server-runtime.md) 中拥有对应边界的位置。
