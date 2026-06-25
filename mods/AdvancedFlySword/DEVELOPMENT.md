# 飞剑术自动连发维护入口

本文面向维护飞剑术自动连发 MOD 源码、组包和发布内容的人。玩家说明见 [README.md](README.md)。

## 能力边界

本 MOD 只有一个后端插件，通过 Harmony 补丁和事件系统实现飞剑术的自动连发逻辑。

- 非入侵：不修改存档数据，所有状态在运行时动态维护。
- 可热卸载：`Dispose` 时清除所有 Harmony 补丁和已注册事件。
- 自动连发触发条件：熟练度 >= 300，且消耗一个式（Trick）后自动连续施展。

## 维护入口

- 后端插件：`src/Backend/BackendPlugin.cs`
- 组件配置：`src/Backend/Taiwu.Mod.props`
- 项目文件：`src/Backend/AdvancedFlySword.Backend.csproj`
- 可部署目录组装：`Taiwu.Mod.Pack.proj`

## 开发

从仓库根目录构建后端插件：

```powershell
dotnet build mods/AdvancedFlySword/src/Backend/AdvancedFlySword.Backend.csproj
```

打包可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name AdvancedFlySword
```

`pack-mod` 会运行 `Taiwu.Mod.Pack.proj`，把 `Config.Lua` 和后端入口 DLL 组装到仓库根目录的
`artifacts/mods/AdvancedFlySword/`。

## 项目结构

- `Config.Lua`：游戏读取的 Mod 配置。
- `Taiwu.Mod.Pack.proj`：最终可部署目录的组包声明。
- `src/Backend/`：后端插件项目，部署到 `Plugins/Backend/`。
  - `BackendPlugin.cs`：插件入口及全部逻辑。
  - `Taiwu.Mod.props`：端侧标记与 Publicizer 配置。
  - `AdvancedFlySword.Backend.csproj`：项目文件。
