# 观象台 Mod 维护入口

本文面向维护观象台源码、组包内容和发布流程的人。使用说明见 [README.md](README.md)。

内部设计入口见 [docs/README.md](docs/README.md)。源码模块边界优先看对应 `src/*/README.md`，不要在本文件复制模块细节。

## 常用命令

从仓库根目录构建插件项目：

```powershell
dotnet build mods/Wanxiang.Guanxiangtai/src/Frontend/Wanxiang.Guanxiangtai.Frontend.csproj
dotnet build mods/Wanxiang.Guanxiangtai/src/McpServer/Wanxiang.Guanxiangtai.McpServer.csproj
```

打包可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name Wanxiang.Guanxiangtai
```

`pack-mod` 会运行 `Taiwu.Mod.Pack.proj`，把它声明的内容组装到仓库根目录的 `artifacts/mods/Wanxiang.Guanxiangtai/`。
当前包内容包括 `Config.Lua`、前端插件入口，以及 `Processes/Wanxiang.Guanxiangtai.McpServer/` 下的 MCP server 发布目录。

## 组包边界

最终包内容由 `Taiwu.Mod.Pack.proj` 声明。新增静态文件、目录、额外项目或发布目录时，先按上一级
[mods/README.md](../README.md) 选择组包 item，再更新组包入口或对应项目文件。

插件项目的本地构建设置写在对应项目的 `Taiwu.Mod.props`。通过 `ProjectReference` 引用的 `shared/` 项目会由 `pack-mod`
自动合并，不在 `Taiwu.Mod.props` 中声明合并或复制动作；非 shared 依赖需要合并或复制时，再通过
`TaiwuModMergeDependency` 和 `TaiwuModCopyDependency` 声明。普通 `dotnet build` 保持项目常规输出。

## 项目结构

- `Config.Lua`：游戏读取的 Mod 配置；字段语义见仓库级文档
  [太吾游戏 Mod 配置与 Steam 发布边界](../../docs/taiwu-mod-steam-publishing-boundary.md)。当前不公开 Mod 设置。
- `Guanxiangtai.Local.json`：用户手工放在 Mod 目录下的本地端口配置文件；组包入口不声明它。
- `Taiwu.Mod.Pack.proj`：最终可部署目录的组包声明。
- `docs/`：内部设计说明，入口见 `docs/README.md`。
- `src/McpServerRuntime/`：前端启动器和 MCP server 本体共享的运行态协调模块，维护运行态入口文件；
  后端插件不引用它。
- `src/Frontend/`：前端插件项目，入口 DLL 默认部署到 `Plugins/`，当前只负责确保 MCP server 进程启动。
- `src/McpServer/`：游戏外 MCP Streamable HTTP server，当前负责常驻 HTTP 入口、鉴权和自身入口注册。

## 同步清单

- 公开配置或面向使用者的说明变化时，同步更新 [README.md](README.md)。
- 包内容变化时，同步更新 `Taiwu.Mod.Pack.proj`、项目文件或项目旁 `Taiwu.Mod.props` 中拥有该声明的位置。
- 非 shared 依赖合并、复制或 Publicizer 设置变化时，同步更新对应项目的 `Taiwu.Mod.props`。
- 运行态入口文件字段、运行目录解析或 MCP 启动方式变化时，同步更新 [docs/mcp-server-runtime.md](docs/mcp-server-runtime.md)
  和 `src/McpServerRuntime/README.md`。
