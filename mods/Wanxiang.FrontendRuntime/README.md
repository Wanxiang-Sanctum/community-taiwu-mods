# 万象前端运行时

太吾绘卷前端共享运行时前置 Mod。

`Wanxiang.FrontendRuntime` 把多个前端 mod 可能共用的 Unity 侧依赖集中到一个先加载的前置 mod 中，
避免下游重复部署同名程序集和重复初始化 UniTask PlayerLoop。

## 运行时范围

当前部署清单由 `src/Frontend/Taiwu.Mod.props` 维护。这个 mod 提供以下前端运行时组件：

- MessagePipe 核心、Interprocess 和 VContainer 集成。
- VContainer。
- UniTask 核心、LINQ、TextMeshPro 和 Addressables 扩展。
- MessagePack 和游戏未提供的辅助程序集。

`System.Buffers.dll`、`System.Memory.dll`、`System.Numerics.Vectors.dll` 和
`System.Runtime.CompilerServices.Unsafe.dll` 已由游戏运行时提供，不在本 mod 中复制部署。

前端入口 DLL 会直接引用本 mod 负责提供的程序集，使太吾的前端插件加载逻辑能按入口 DLL 的直接引用从
`Plugins/` 目录预加载这些依赖。插件初始化时仅在 UniTask PlayerLoop 尚未注入时，通过 UniTask 公开
API 注入当前 Unity PlayerLoop。

## 下游使用

需要这些运行时的前端 mod 应将本 mod 设为依赖，并只保留编译期引用，不再重复部署同名运行时 DLL。
下游前端项目引用这些包时建议排除 runtime 资产：

```xml
<PackageReference
  Include="Taiwu.ModKit.Dependencies.MessagePipe"
  ExcludeAssets="runtime"
  PrivateAssets="all"
/>
```

`Taiwu.ModKit.Dependencies.UniTask`、`Taiwu.ModKit.Dependencies.MessagePipe.Interprocess`、
`Taiwu.ModKit.Dependencies.MessagePipe.VContainer` 和 `Taiwu.ModKit.Dependencies.VContainer`
同样按这个方式引用。发布后，将“万象前端运行时”的 Steam Workshop `FileId` 加入下游 mod 的
`Dependencies`，确保本 mod 先于下游 mod 加载。

## 维护边界

本 mod 自身的项目引用以 `Taiwu.ModKit.Dependencies.*` 为主；`TaiwuModCopyDependency` 是随包部署
DLL 的显式清单。只有当入口 DLL 需要直接引用某个辅助程序集，且该程序集没有作为可编译引用从上游包传递
下来时，才额外写直接 `PackageReference`。

`Microsoft.NET.StringTools.dll` 是当前的例外：它会随 `MessagePack` 进入 runtime/copy-local 输出，
但不会作为 compile asset 传递，因此这里直接引用它来生成入口 DLL 的直接程序集引用。这个包的版本由仓库
根目录的 `Directory.Packages.props` 统一管理。

## 开发

从仓库根目录构建插件项目：

```powershell
dotnet build mods/Wanxiang.FrontendRuntime/src/Frontend/Wanxiang.FrontendRuntime.Frontend.csproj
dotnet build mods/Wanxiang.FrontendRuntime/src/Backend/Wanxiang.FrontendRuntime.Backend.csproj
```

打包可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name Wanxiang.FrontendRuntime
```

`pack-mod` 会把 `Config.Lua`、插件入口 DLL 和显式声明的前端运行时 DLL 组装到仓库根目录的
`artifacts/mods/Wanxiang.FrontendRuntime/`。

插件项目的本地构建设置写在对应项目的 `Taiwu.Mod.props`。前端项目在这里声明需要随包部署的
共享运行时 DLL；这些部署声明只由 `pack-mod` 使用。后端项目是为了适配仓库当前前后端成对打包约定的
空入口，不承载共享后端依赖。

## 项目结构

- `Config.Lua`：游戏读取的 mod 配置。
- `src/Frontend/`：前端插件项目。
- `src/Backend/`：后端占位插件项目。
