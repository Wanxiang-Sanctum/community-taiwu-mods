# 万象引维护入口

本文面向维护万象引源码、运行时部署清单和发布内容的人。玩家和订阅者说明见 `README.md`。

## 能力边界

具体 DLL 部署清单以 `src/Frontend/Taiwu.Mod.props` 和 `src/Backend/Taiwu.Mod.props` 为准。本文说明运行时类别和维护入口；
不由万象引部署的程序集，归游戏运行时、依赖方部署或编译期引用边界处理。

万象引引用的 `Taiwu.ModKit.Dependencies.*` 包由组织内部
[`taiwu-modkit`](https://github.com/Wanxiang-Sanctum/taiwu-modkit) 仓库的 UPM 依赖包工具生成和发布；本 Mod 在版本文件中
固定包版本，并声明实际部署动作。需要调整 Unity/UPM 依赖包内容时，先改该内部仓库的工具配置；需要调整本 Mod 携带哪些
运行时 DLL 时，再修改本目录下的项目文件和 `Taiwu.Mod.props`。

实现细节集中在 `src/PluginLoading/`；玩家 README 不列出补丁点和解析器内部流程。

## 依赖方使用

依赖方项目引用万象引已提供的运行时包时，保留编译期引用，并让同名运行时 DLL 由万象引提供。常见做法是在
`PackageReference` 上排除 runtime 资产：

```xml
<PackageReference
  Include="Taiwu.ModKit.Dependencies.MessagePipe"
  ExcludeAssets="runtime"
  PrivateAssets="all"
/>
```

如果依赖方确实需要携带自己的版本，应把入口和相关复制依赖部署到同一个 `Plugins/` 子目录，并在实际游戏环境中验证解析结果。

发布时，将万象引的 Steam Workshop `FileId` 加入依赖方 Mod 的 `Dependencies`，确保万象引先于依赖方 Mod 加载。
插件入口和复制依赖部署到子目录的 MSBuild 写法见 `../README.md`。

## 维护入口

- 运行时部署清单：`src/Frontend/Taiwu.Mod.props`、`src/Backend/Taiwu.Mod.props`。
- 插件加载策略：`src/PluginLoading/`。
- NuGet 版本：仓库根目录的 `Directory.Packages.props` 和各项目 `packages.lock.json`。
- 可部署目录组装：`Taiwu.Mod.Pack.proj`。

新增或移除共享运行时时，优先更新项目文件和 lock file；玩家 README 只在运行时类别、使用约束或订阅边界变化时同步调整。

## 开发

从仓库根目录构建插件项目：

```powershell
dotnet build mods/Wanxiang.Prelude/src/Frontend/Wanxiang.Prelude.Frontend.csproj
dotnet build mods/Wanxiang.Prelude/src/Backend/Wanxiang.Prelude.Backend.csproj
```

打包可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name Wanxiang.Prelude
```

`pack-mod` 会运行 `Taiwu.Mod.Pack.proj`，把 `Config.Lua`、前后端入口 DLL 和显式声明的运行时 DLL 组装到仓库根目录的
`artifacts/mods/Wanxiang.Prelude/`。

## 项目结构

- `Config.Lua`：游戏读取的 Mod 配置。
- `Taiwu.Mod.Pack.proj`：最终可部署目录的组包声明。
- `src/Frontend/`：前端插件项目，部署到 `Plugins/Frontend/`。
- `src/Backend/`：后端插件项目，部署到 `Plugins/Backend/`。
- `src/PluginLoading/`：前后端共用的插件加载桥接项目。
