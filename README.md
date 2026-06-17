# Community Taiwu Mods

太吾绘卷实际 mod 仓库。

这个仓库维护可直接构建、打包和发布的 mod，以及这些 mod 之间复用的内部共享项目。它保留
`Taiwu.Mods.Cli`、`templates/` 和共享 MSBuild 目标，用来在本仓库内继续新增 mod 或内部共享项目。
模板仓库本身由 [`taiwu-mods`](https://github.com/Wanxiang-Sanctum/taiwu-mods) 维护。

## 文档边界

仓库根 README 负责说明仓库入口、跨目录约定和外部依赖关系。具体 mod 的玩法、运行链路、工作区内容、源码模块和
维护入口由对应 `mods/<ModName>/README.md` 及其子目录 README 维护；内部共享项目由
`shared/<ProjectName>/README.md` 维护。

本仓库按两种方式使用 [`taiwu-modkit`](https://github.com/Wanxiang-Sanctum/taiwu-modkit)：

- 本仓库引用 `Taiwu.ModKit.References.*` 和 `Taiwu.ModKit.Dependencies.*` NuGet 包。包切分、打包和发布流程归
  `taiwu-modkit` 的工具与配置维护；本仓库通过 `Directory.Packages.props` 固定版本，并通过
  `NuGet.config` 配置包源。
- 源码维护时，可以把 `taiwu-modkit` 的 `game/` 生成快照作为检索、跳转和变更观察证据。运行时内容以本仓库的
  mod 源码、组包入口和发布产物为准；快照需要更新时，在 `taiwu-modkit` 中运行对应工具重新生成。

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
- `tools/Taiwu.Mods.Cli/`：创建 mod、内部共享项目、取消解决方案注册和打包可部署目录的命令行工具。
- `.github/workflows/`：GitHub Actions 工作流，覆盖 PR 验证和 mod release 打包。
- `artifacts/mods/`：`pack-mod` 输出的可部署目录。
- `Taiwu.Mods.Paths.props`：仓库级 MSBuild 路径 alias，供子目录 props 和项目引用稳定目录。
- `Taiwu.Mods.slnx`：解决方案入口，收录工具、已注册的 mod 项目和内部共享项目。
- `Directory.Build.props`：仓库级编译、分析器和代码质量规则。
- `Directory.Packages.props`：NuGet 包版本。
- `NuGet.config`：NuGet 包源、包源映射，以及从环境变量读取 GitHub Packages 凭据的配置。

## 仓库维护

需要检查或格式化仓库文档、配置和项目文件时运行：

```powershell
dotnet msbuild repo.proj -t:Check
dotnet msbuild repo.proj -t:Format
```

这些目标通过 `aqua` 调用仓库声明的维护工具。本机没有 `aqua` 时，Windows 可用 `winget install aquaproj.aqua`
或 `scoop install aqua`。如需提前安装这些工具，运行：

```powershell
dotnet msbuild repo.proj -t:InstallTools
```

更新 `aqua.yml` 中的工具版本后，同步刷新校验文件：

```powershell
dotnet msbuild repo.proj -t:UpdateToolChecksums
```
