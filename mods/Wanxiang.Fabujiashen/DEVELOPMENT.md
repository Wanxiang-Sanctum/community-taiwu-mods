# Wanxiang.Fabujiashen 维护入口

本文面向维护法不加身源码、组包内容和发布流程的人。玩家说明见 [README.md](README.md)。

## 常用命令

从仓库根目录构建前端或后端插件项目：

```powershell
dotnet build mods/Wanxiang.Fabujiashen/src/Frontend/Wanxiang.Fabujiashen.Frontend.csproj
dotnet build mods/Wanxiang.Fabujiashen/src/Backend/Wanxiang.Fabujiashen.Backend.csproj
```

打包可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name Wanxiang.Fabujiashen
```

`pack-mod` 会运行 `Taiwu.Mod.Pack.proj`，把它声明的内容组装到仓库根目录的 `artifacts/mods/Wanxiang.Fabujiashen/`。

## 实现边界

- `src/Frontend/FrontendPlugin.cs`：前端插件入口，注册只在太吾人物特性列表显示的“法不加身”虚拟特性。
- `src/Frontend/Taiwu.Mod.props`：声明前端侧，并把 `Wanxiang.Taiwu.PlayerVisibleFeatures` 合并进入口 DLL。
- `src/Backend/BackendPlugin.cs`：插件入口，只安装和卸载 Harmony patch。
- `src/Backend/FabujiashenRules.cs`：运行时规则模型，集中维护太吾身份、战斗角色塑形、公共入口允许/拒绝规则和真实人物伤毒兜底规则。
- `src/Backend/FabujiashenPatches.cs`：Harmony 安装、卸载和 patch 接入点。patch 类只负责绑定游戏入口，并把规则判断交给 `FabujiashenRules`。
- `src/Backend/SpecialEffectFatalSourceMethods.cs`：定位特殊效果中会调用致命伤或致命标记入口的方法，供源侧作用域 patch 使用；它不拥有规则判断。
- `src/Backend/Taiwu.Mod.props`：声明后端侧，并 publicize `GameData`，让补丁可以用 `nameof(...)` 强类型引用游戏非 public 成员。
- `docs/design.md`：记录三层设计、游戏侧复用、AI 可见性、Publicizer 选择和边界。

本 Mod 前端只提供玩家可见的虚拟特性显示，不提供设置项，也不写入人物真实 `FeatureIds`。

## 同步清单

- 面向玩家的效果、边界或兼容说明变化时，同步更新 [README.md](README.md) 和 `Config.Lua` 的 `Description`。
- 实现策略、Publicizer 范围、游戏 API 复用边界、战斗角色塑形边界或真实人物兜底边界变化时，同步更新 [docs/design.md](docs/design.md)。
- 包内容变化时，同步更新 `Taiwu.Mod.Pack.proj`。
- 补丁新增对其它程序集非 public 游戏 API 的直接调用时，再评估是否需要扩展 `Publicize` 项。
- 需要扩展到非战斗活动时，先确认对应活动是否也创建阶段性角色对象；不要把战斗角色对象假设直接套到其它域。真实人物兜底只覆盖明确会持久化的伤毒写入入口。
