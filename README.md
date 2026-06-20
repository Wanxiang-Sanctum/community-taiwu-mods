# Community Taiwu Mods

[![Ask Zread](https://img.shields.io/badge/Ask_Zread-_.svg?style=flat&color=00b0aa&labelColor=000000&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdCb3g9IjAgMCAxNiAxNiIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHBhdGggZD0iTTQuOTYxNTYgMS42MDAxSDIuMjQxNTZDMS44ODgxIDEuNjAwMSAxLjYwMTU2IDEuODg2NjQgMS42MDE1NiAyLjI0MDFWNC45NjAxQzEuNjAxNTYgNS4zMTM1NiAxLjg4ODEgNS42MDAxIDIuMjQxNTYgNS42MDAxSDQuOTYxNTZDNS4zMTUwMiA1LjYwMDEgNS42MDE1NiA1LjMxMzU2IDUuNjAxNTYgNC45NjAxVjIuMjQwMUM1LjYwMTU2IDEuODg2NjQgNS4zMTUwMiAxLjYwMDEgNC45NjE1NiAxLjYwMDFaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00Ljk2MTU2IDEwLjM5OTlIMi4yNDE1NkMxLjg4ODEgMTAuMzk5OSAxLjYwMTU2IDEwLjY4NjQgMS42MDE1NiAxMS4wMzk5VjEzLjc1OTlDMS42MDE1NiAxNC4xMTM0IDEuODg4MSAxNC4zOTk5IDIuMjQxNTYgMTQuMzk5OUg0Ljk2MTU2QzUuMzE1MDIgMTQuMzk5OSA1LjYwMTU2IDE0LjExMzQgNS42MDE1NiAxMy43NTk5VjExLjAzOTlDNS42MDE1NiAxMC42ODY0IDUuMzE1MDIgMTAuMzk5OSA0Ljk2MTU2IDEwLjM5OTlaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik0xMy43NTg0IDEuNjAwMUgxMS4wMzg0QzEwLjY4NSAxLjYwMDEgMTAuMzk4NCAxLjg4NjY0IDEwLjM5ODQgMi4yNDAxVjQuOTYwMUMxMC4zOTg0IDUuMzEzNTYgMTAuNjg1IDUuNjAwMSAxMS4wMzg0IDUuNjAwMUgxMy43NTg0QzE0LjExMTkgNS42MDAxIDE0LjM5ODQgNS4zMTM1NiAxNC4zOTg0IDQuOTYwMVYyLjI0MDFDMTQuMzk4NCAxLjg4NjY0IDE0LjExMTkgMS42MDAxIDEzLjc1ODQgMS42MDAxWiIgZmlsbD0iI2ZmZiIvPgo8cGF0aCBkPSJNNCAxMkwxMiA0TDQgMTJaIiBmaWxsPSIjZmZmIi8%2BCjxwYXRoIGQ9Ik00IDEyTDEyIDQiIHN0cm9rZT0iI2ZmZiIgc3Ryb2tlLXdpZHRoPSIxLjUiIHN0cm9rZS1saW5lY2FwPSJyb3VuZCIvPgo8L3N2Zz4K&logoColor=ffffff)](https://zread.ai/Wanxiang-Sanctum/community-taiwu-mods)

太吾绘卷实际 mod 仓库。

这个仓库维护可直接构建、打包和发布的 mod，以及这些 mod 之间复用的内部共享项目。它保留
`Taiwu.Mods.Cli`、`templates/` 和共享 MSBuild 目标，用来在本仓库内继续新增 mod 或内部共享项目。
模板仓库本身由 [`taiwu-mods`](https://github.com/Wanxiang-Sanctum/taiwu-mods) 维护。

## 文档边界

仓库根 README 负责说明仓库身份、常用命令、跨目录约定和外部依赖关系。仓库级 `docs/` 负责沉淀跨具体
Mod 复用的太吾机制、平台机制、发布经验和维护判断。目录级 README 负责各自的共同规则：`mods/README.md` 说明所有
mod 的组包和插件规则，`shared/README.md` 说明内部共享项目边界，`templates/README.md` 说明模板变量和
渲染规则，`tools/README.md` 说明仓库维护工具的实现入口。

具体 mod 的玩法、运行链路、工作区内容、源码模块和维护入口由对应 `mods/<ModName>/README.md` 及其子目录
README 维护；内部共享项目自己的 API、部署建议和维护入口由 `shared/<ProjectName>/README.md` 维护。

本仓库是公开的实际 mod 仓库；[`taiwu-modkit`](https://github.com/Wanxiang-Sanctum/taiwu-modkit) 是
Wanxiang-Sanctum 组织内部维护太吾 mod 开发辅助工具、引用包和游戏观察快照的仓库。涉及游戏机制、游戏文本、
运行时行为或 Steam Workshop 语义时，文档以太吾绘卷游戏本体和对应外部平台为依据；`taiwu-modkit` 中的快照承担
组织内部检索、跳转和变更对照。

本仓库按两种方式使用 `taiwu-modkit`：

- 本仓库引用 `Taiwu.ModKit.References.*` 和 `Taiwu.ModKit.Dependencies.*` NuGet 包。包切分、打包和发布流程归
  `taiwu-modkit` 的工具与配置维护；本仓库通过 `Directory.Packages.props` 固定版本，并通过
  `NuGet.config` 配置包源。
- 源码维护时，组织内部维护者使用 `taiwu-modkit` 仓库根目录下的 `game/` 生成快照对照太吾游戏文件和源码观察结果。
  运行时内容以本仓库的 mod 源码、组包入口和发布产物为准；游戏观察快照需要更新时，在
  `taiwu-modkit` 中运行对应工具重新生成。

## 项目命令

恢复解决方案依赖：

```powershell
dotnet restore Taiwu.Mods.slnx
```

解决方案里的 mod 项目会下载 GitHub Packages 上的 `Taiwu.ModKit.*` 包。需要准备有 `read:packages` 权限的
GitHub classic personal access token，并在当前 PowerShell 会话中提供给 NuGet：

```powershell
$env:TAIWU_MODKIT_GITHUB_USER = "<GitHubUser>"
$env:TAIWU_MODKIT_GITHUB_TOKEN = "<GitHubToken>"
dotnet restore Taiwu.Mods.slnx
```

仓库启用 NuGet lock file，用于固定每个项目的 NuGet 依赖解析结果。新增项目、调整 `PackageReference`
或更新 `Directory.Packages.props` 后，运行 restore 并提交对应项目目录下生成或更新的 `packages.lock.json`。
CI 使用 locked restore 校验依赖声明和 lock file 是否一致。

构建解决方案：

```powershell
dotnet build Taiwu.Mods.slnx
```

打包某个 mod 的可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name Wanxiang.Xiangshu
```

`pack-mod` 默认使用 `Release` 运行 `mods/<ModName>/Taiwu.Mod.Pack.proj`，并把该组包入口声明的文件、目录和
项目产物组装到 `artifacts/mods/<ModName>/`。这个目录可直接替换游戏内对应 mod 目录；组包声明、插件入口、
依赖部署和发布目录项目约定见 `mods/README.md`。

发布到 GitHub Release：

```powershell
git tag mods/<ModName>/v<Version>
git push origin mods/<ModName>/v<Version>
```

`mods/<ModName>/v<Version>` 是仓库的发布 tag 约定。推送后，GitHub Actions 会以 `<ModName>` 运行
`pack-mod`，上传 `<ModName>-<Version>.zip` 到对应 GitHub Release。zip 内包含可直接替换游戏 mod 目录的
`<ModName>/` 目录；`ModName` 必须与 `mods/` 下的一级目录名一致。

## 新增项目

新增实际 mod：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- create-mod --name MyMod
```

`ModName` 必须是 C# 命名空间风格的标识符，例如 `MyMod` 或 `MyCompany.MyMod`。创建后，生成器会复制
`templates/mod/`，渲染模板变量，并把模板内项目加入 `Taiwu.Mods.slnx`。生成的 `Taiwu.Mod.Pack.proj` 是该
mod 的可部署目录组包入口。

新增内部共享项目：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- create-shared --name MyCompany.Taiwu.Shared
```

共享项目默认使用 `Shared` 端侧，适合纯共享抽象和通用实现。如果项目面向前端或后端，可以显式指定端侧来
选择默认目标框架：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- create-shared --name MyCompany.Taiwu.FrontendSupport --side Frontend
dotnet run --project tools/Taiwu.Mods.Cli -- create-shared --name MyCompany.Taiwu.BackendSupport --side Backend
```

从解决方案取消注册某个 mod，但保留文件：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- remove-mod --name MyMod
```

从解决方案取消注册某个内部共享项目，但保留文件：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- remove-shared --name MyCompany.Taiwu.Shared
```

## 仓库结构

- `mods/`：实际 mod 源码目录。组包声明、前后端插件项目、Taiwu 引用、Publicizer、插件依赖和发布目录项目
  约定见 `mods/README.md`。
- `shared/`：内部共享项目目录。共享边界、目标框架和项目级配置入口见 `shared/README.md`。
- `templates/`：命令行工具创建项目时使用的 Scriban 模板。模板维护约定见 `templates/README.md`。
- `tools/`：创建 mod、内部共享项目、取消解决方案注册和打包可部署目录的命令行工具，工具文档关系见
  `tools/README.md`。
- `.github/workflows/`：GitHub Actions 工作流，覆盖 PR 验证和 mod release 打包。
- `docs/`：跨具体 Mod 复用的太吾机制、平台机制、发布经验和维护判断；入口见 `docs/README.md`。
- `artifacts/mods/`：`pack-mod` 输出的可部署目录。
- `Taiwu.Mods.Paths.props`：仓库级 MSBuild 路径 alias，供子目录 props 和项目引用稳定目录。
- `Taiwu.Mods.slnx`：解决方案入口，收录工具、已注册的 mod 项目和内部共享项目。
- `Directory.Build.props`：仓库级编译、分析器和代码质量规则。
- `Directory.Packages.props`：NuGet 包版本。
- `NuGet.config`：NuGet 包源、包源映射，以及从环境变量读取 GitHub Packages 凭据的配置。

## 阅读入口

- `mods/README.md`：mod 共同遵守的组包、插件入口、引用和部署规则。
- `shared/README.md`：内部共享项目边界、目标框架和项目级配置入口。
- `templates/README.md`：模板目录、模板变量和渲染规则。
- `tools/README.md`：仓库命令行工具的实现入口。
- `docs/README.md`：跨具体 Mod 复用的太吾机制、平台机制、发布经验和维护判断。
- `CONTRIBUTING.md`：维护本仓库文档、模板和工具时使用的规则。
