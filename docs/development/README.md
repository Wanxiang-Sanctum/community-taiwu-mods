# 开发维护入口

本文面向维护本仓库实际 Mod 源码、文档、组包流程和发布流程的人。玩家安装、使用和风险边界从根 `README.md` 与具体 Mod
的 `README.md` 开始；提交贡献前的协作入口见根 `CONTRIBUTING.md`。

## 阅读路径

| 任务 | 入口 |
| --- | --- |
| 构建、打包、发布现有 Mod | 本文 |
| 维护所有 Mod 共同的组包、插件入口、依赖部署规则 | `../../mods/README.md` |
| 维护某个 Mod 的源码模块、发布内容和内部设计 | `../../mods/<ModName>/DEVELOPMENT.md` |
| 维护内部共享项目 | `../../shared/README.md` |
| 维护创建/移除命令实现或模板 | `../../tools/README.md`、`../../templates/README.md` |
| 维护文档分层和同步规则 | `documentation.md` |
| 维护跨 Mod 复用的机制参考或仓库经验 | `../README.md` |

具体 Mod 的 `README.md` 面向外部技术玩家，说明使用方式、运行边界和配置入口。内部设计、构建命令、组包细节和源码模块
入口放在该 Mod 的 `DEVELOPMENT.md` 或源码子目录 README。

## 外部依据

[`Taiwu.Mods`](https://github.com/Wanxiang-Sanctum/taiwu-mods) 是本仓库的太吾 Mod monorepo 模板来源。本仓库内的
`templates/`、`tools/`、MSBuild 骨架和 GitHub Actions 以实际 Mod 需要为准保留本地适配；通用 monorepo 模板能力、
模板仓库文档和从模板创建新仓库的说明由 `Taiwu.Mods` 维护。

[`taiwu-modkit`](https://github.com/Wanxiang-Sanctum/taiwu-modkit) 是 Wanxiang-Sanctum 组织内部维护太吾 mod
开发辅助工具、引用包和游戏观察快照的仓库。

本仓库按两种方式使用它：

- 本仓库引用 `Taiwu.ModKit.References.*` 和 `Taiwu.ModKit.Dependencies.*` NuGet 包。包切分、打包和发布流程归
  `taiwu-modkit` 的工具与配置维护；本仓库通过 `Directory.Packages.props` 固定版本，并通过 `NuGet.config` 配置包源。
- 维护源码时，可使用 `taiwu-modkit` 仓库根目录下的 `game/` 生成快照对照太吾游戏文件和源码观察结果。
  运行时内容以本仓库的 Mod 源码、组包入口和发布产物为准；游戏观察快照需要更新时，在 `taiwu-modkit` 中运行对应工具
  重新生成。

## 环境与依赖

恢复解决方案依赖：

```powershell
dotnet restore Taiwu.Mods.slnx
```

解决方案里的 Mod 项目会下载 GitHub Packages 上的 `Taiwu.ModKit.*` 包。需要准备有 `read:packages` 权限的 GitHub
classic personal access token，并在当前 PowerShell 会话中提供给 NuGet：

```powershell
$env:TAIWU_MODKIT_GITHUB_USER = "<GitHubUser>"
$env:TAIWU_MODKIT_GITHUB_TOKEN = "<GitHubToken>"
dotnet restore Taiwu.Mods.slnx
```

仓库启用 NuGet lock file，用于固定每个项目的 NuGet 依赖解析结果。新增项目、调整 `PackageReference` 或更新
`Directory.Packages.props` 后，运行 restore 并提交对应项目目录下生成或更新的 `packages.lock.json`。CI 使用
locked restore 校验依赖声明和 lock file 是否一致。

## 构建与检查

构建解决方案：

```powershell
dotnet build Taiwu.Mods.slnx
```

检查或格式化仓库文档、配置和项目文件：

```powershell
dotnet msbuild repo.proj -t:Check
dotnet msbuild repo.proj -t:Format
```

这些目标通过 `aqua` 调用仓库声明的维护工具。本机没有 `aqua` 时，Windows 可用 `winget install aquaproj.aqua` 或
`scoop install aqua`。如需提前安装这些工具，运行：

```powershell
dotnet msbuild repo.proj -t:InstallTools
```

更新 `aqua.yml` 中的工具版本后，同步刷新校验文件：

```powershell
dotnet msbuild repo.proj -t:UpdateToolChecksums
```

## 打包与发布

打包某个 Mod 的可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name Wanxiang.Xiangshu
```

`pack-mod` 默认使用 `Release` 运行 `mods/<ModName>/Taiwu.Mod.Pack.proj`，并把该组包入口声明的文件、目录和项目产物
组装到 `artifacts/mods/<ModName>/`。这个目录可直接替换游戏内对应 Mod 目录；组包声明、插件入口、依赖部署和发布目录
项目约定见 `mods/README.md`。

发布到 GitHub Release：

```powershell
git tag mods/<ModName>/v<Version>
git push origin mods/<ModName>/v<Version>
```

`mods/<ModName>/v<Version>` 是仓库的发布 tag 约定。推送后，GitHub Actions 会以 `<ModName>` 运行 `pack-mod`，
上传 `<ModName>-<Version>.zip` 到对应 GitHub Release。zip 内包含可直接替换游戏 Mod 目录的 `<ModName>/` 目录；
`ModName` 必须与 `mods/` 下的一级目录名一致。

## 新增和移除项目

本仓库的创建命令用于扩展仓库内项目。新项目生成后，真实构建、组包和部署约定由生成出的项目文件、
`Taiwu.Mod.Pack.proj`、目录 README、lock file 和解决方案注册共同维护。

新增实际 Mod：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- create-mod --name MyMod
```

`ModName` 必须是 C# 命名空间风格的标识符，例如 `MyMod` 或 `MyCompany.MyMod`。创建后，生成器会复制
`templates/mod/`，渲染模板变量，并把模板内项目加入 `Taiwu.Mods.slnx`。生成的 `Taiwu.Mod.Pack.proj` 是该 Mod
的可部署目录组包入口。

新增内部共享项目：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- create-shared --name MyCompany.Taiwu.Shared
```

共享项目默认使用 `Shared` 端侧，适合纯共享抽象和通用实现。如果项目面向前端或后端，可以显式指定端侧来选择默认目标框架：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- create-shared --name MyCompany.Taiwu.FrontendSupport --side Frontend
dotnet run --project tools/Taiwu.Mods.Cli -- create-shared --name MyCompany.Taiwu.BackendSupport --side Backend
```

从解决方案取消注册某个 Mod，但保留文件：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- remove-mod --name MyMod
```

从解决方案取消注册某个内部共享项目，但保留文件：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- remove-shared --name MyCompany.Taiwu.Shared
```

新增、移除或重命名实际 Mod 时，同步更新 `mods/README.md` 的一级目录索引。新增、移除或重命名内部共享项目时，同步更新
`shared/README.md` 的一级目录索引。根 README 只在对外 Mod 入口需要变化时更新；开发手册和机制参考需要项目发现时，链接
目录级 README。

## 仓库结构

- `mods/`：实际 Mod 源码目录。一级目录索引、Mod 目录约定、组包声明、插件项目、Taiwu 引用、Publicizer、插件依赖和
  发布目录项目约定见 `mods/README.md`。
- `shared/`：本仓库内部共享项目目录。一级目录索引、共享项目目录约定、共享边界、目标框架和项目级配置入口见
  `shared/README.md`。
- `docs/`：维护本仓库实际 Mod 时使用的跨 Mod 机制参考、仓库经验和开发维护文档。
- `tools/`：本仓库辅助命令行工具，负责创建项目、取消解决方案注册和打包可部署目录；实现入口见 `tools/README.md`。
- `templates/`：本仓库创建命令使用的 Scriban 初始骨架；变量和渲染规则见 `templates/README.md`。
- `.github/workflows/`：GitHub Actions 工作流，覆盖 PR 验证和 Mod release 打包。
- `artifacts/mods/`：`pack-mod` 输出的可部署目录；手写源码从 `mods/`、`shared/` 和 `tools/` 进入。
- `Taiwu.Mods.Paths.props`：仓库级 MSBuild 路径 alias，供子目录 props 和项目引用稳定目录。
- `Taiwu.Mods.slnx`：解决方案入口，收录工具、已注册的 Mod 项目和内部共享项目。
- `Directory.Build.props`：仓库级编译、分析器和代码质量规则。
- `Directory.Packages.props`：NuGet 包版本。
- `NuGet.config`：NuGet 包源、包源映射，以及从环境变量读取 GitHub Packages 凭据的配置。
