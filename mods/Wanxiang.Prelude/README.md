# 万象引

万象系 Mod 的前置引导层。

`Wanxiang.Prelude` 承担两项基础职责：

- 在前端和后端分别部署万象系 Mod 共用的运行时。
- 在自身加载后，为后续 Mod 提供按插件入口所在目录优先的 DLL 解析规则。

它不提供玩家入口，也不承载具体玩法或工具语义。需要这些运行时或加载规则的 Mod 应声明依赖万象引。

## 能力边界

万象引按侧端维护共享运行时：

- 前端侧覆盖 IPC、序列化、容器、异步任务，以及游戏前端未提供但前端共享包需要的支撑程序集。
- 后端侧覆盖 IPC、序列化、脚本编译和依赖注入所需的共享运行时。

具体 DLL 部署清单以 `src/Frontend/Taiwu.Mod.props` 和 `src/Backend/Taiwu.Mod.props` 为准。README 只说明
边界，不复制完整清单；游戏运行时已经提供、下游自行携带或只用于编译期的程序集，也不在这里逐项列出。

## 插件加载规则

太吾原生加载器会从 Mod 的 `Plugins/` 目录读取 `Config.Lua` 中声明的前后端插件入口。入口路径可以包含
子目录，但原生依赖预加载仍以 `Plugins/` 根目录为主要查找位置，容易让前后端或多个插件的同名 DLL 相互
挤占。

万象引加载后，后续 Mod 的插件依赖解析遵循这些规则：

- 入口 DLL 按 `Config.Lua` 中声明的相对路径读取。
- 加载入口时会注册入口程序集及其从插件目录解析到的本地依赖图。
- 依赖解析优先使用入口 DLL 所在目录，再回退到 `Plugins/` 根目录。
- 同一路径已加载的依赖会复用；不同入口目录下的同名 DLL 会优先按请求方目录分别解析。

这条规则只影响万象引加载之后的 Mod。需要它的下游 Mod 必须把万象引声明为前置依赖，并让自己的入口和
需要隔离的复制依赖放在同一个插件子目录下。

实现细节集中在 `src/PluginLoading/`；根 README 不列出补丁点和解析器内部流程。

## 下游使用

下游项目引用万象引提供的运行时包时，保留编译期引用，但避免再次部署同名运行时 DLL。常见做法是在
`PackageReference` 上排除 runtime 资产：

```xml
<PackageReference
  Include="Taiwu.ModKit.Dependencies.MessagePipe"
  ExcludeAssets="runtime"
  PrivateAssets="all"
/>
```

如果下游确实需要携带自己的版本，应把入口和相关复制依赖部署到同一个 `Plugins/` 子目录，并在实际游戏
环境中验证解析结果。

发布时，将万象引的 Steam Workshop `FileId` 加入下游 Mod 的 `Dependencies`，确保万象引先于下游 Mod
加载。插件入口和复制依赖部署到子目录的 MSBuild 写法见 `mods/README.md`。

## 维护入口

- 运行时部署清单：`src/Frontend/Taiwu.Mod.props`、`src/Backend/Taiwu.Mod.props`。
- 插件加载策略：`src/PluginLoading/`。
- NuGet 版本：仓库根目录的 `Directory.Packages.props` 和各项目 `packages.lock.json`。
- 可部署目录组装：`Taiwu.Mod.Pack.proj`。

新增或移除共享运行时时，优先更新项目文件和 lock file；README 只在运行时类别、使用约束或维护入口变化时
同步调整。

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

`pack-mod` 会运行 `Taiwu.Mod.Pack.proj`，把 `Config.Lua`、前后端入口 DLL 和显式声明的运行时 DLL
组装到仓库根目录的 `artifacts/mods/Wanxiang.Prelude/`。

## 项目结构

- `Config.Lua`：游戏读取的 Mod 配置。
- `Taiwu.Mod.Pack.proj`：最终可部署目录的组包声明。
- `src/Frontend/`：前端插件项目，部署到 `Plugins/Frontend/`。
- `src/Backend/`：后端插件项目，部署到 `Plugins/Backend/`。
- `src/PluginLoading/`：前后端共用的插件加载桥接项目。
