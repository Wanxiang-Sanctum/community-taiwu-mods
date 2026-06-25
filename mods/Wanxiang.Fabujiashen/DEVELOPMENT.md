# Wanxiang.Fabujiashen 维护入口

本文面向维护法不加身源码、组包内容和发布流程的人。玩家说明见 [README.md](README.md)。

## 常用命令

从仓库根目录构建后端插件项目：

```powershell
dotnet build mods/Wanxiang.Fabujiashen/src/Backend/Wanxiang.Fabujiashen.Backend.csproj
```

打包可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name Wanxiang.Fabujiashen
```

`pack-mod` 会运行 `Taiwu.Mod.Pack.proj`，把它声明的内容组装到仓库根目录的 `artifacts/mods/Wanxiang.Fabujiashen/`。

## 实现边界

- `src/Backend/BackendPlugin.cs`：插件入口，只安装和卸载 Harmony patch。
- `src/Backend/FabujiashenPatches.cs`：运行时规则和 Harmony patch 实现。它按“战斗角色塑形 + 公共入口拦截”两层组织，具体行为边界由 [docs/design.md](docs/design.md) 维护。
- `src/Backend/Taiwu.Mod.props`：声明后端侧，并 publicize `GameData`，让补丁可以用 `nameof(...)` 强类型引用游戏非 public 成员。
- `docs/design.md`：记录两层设计、游戏侧复用、AI 可见性、Publicizer 选择和边界。

本 Mod 当前没有前端插件，也没有随包复制的额外依赖。

## 同步清单

- 面向玩家的效果、边界或兼容说明变化时，同步更新 [README.md](README.md) 和 `Config.Lua` 的 `Description`。
- 实现策略、Publicizer 范围、游戏 API 复用边界或战斗角色塑形边界变化时，同步更新 [docs/design.md](docs/design.md)。
- 包内容变化时，同步更新 `Taiwu.Mod.Pack.proj`。
- 补丁新增对其它程序集非 public 游戏 API 的直接调用时，再评估是否需要扩展 `Publicize` 项。
- 需要扩展到非战斗活动时，先确认对应活动是否也创建阶段性角色对象；不要把战斗角色对象假设直接套到其它域。
