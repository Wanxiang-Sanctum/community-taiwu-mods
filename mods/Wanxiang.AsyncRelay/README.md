# 万象续流

太吾绘卷 UniTask 前置 Mod。

`Wanxiang.AsyncRelay` 会在前端加载时携带并加载 UniTask 运行时程序集：

- `UniTask.dll`
- `UniTask.Linq.dll`
- `UniTask.TextMeshPro.dll`
- `UniTask.Addressables.dll`

同时它会手动触发 UniTask 的 Unity 环境初始化，补上动态加载 mod 时不会自动执行的
`RuntimeInitializeOnLoadMethod` 流程。其他需要 UniTask 的前端 mod 可以将本 mod 设为依赖，
然后只保留编译期引用，不再重复部署 UniTask 运行时 DLL，也不需要自己初始化 `PlayerLoopHelper`。

下游前端 mod 引用 UniTask 时建议排除 runtime 资产：

```xml
<PackageReference
  Include="Taiwu.ModKit.Dependencies.UniTask"
  ExcludeAssets="runtime"
  PrivateAssets="all"
/>
```

发布后，将“万象续流”的 Steam Workshop `FileId` 加入下游 mod 的 `Dependencies`，确保本 mod
先于下游 mod 加载。

## 开发

从仓库根目录构建插件项目：

```powershell
dotnet build mods/Wanxiang.AsyncRelay/src/Frontend/Wanxiang.AsyncRelay.Frontend.csproj
dotnet build mods/Wanxiang.AsyncRelay/src/Backend/Wanxiang.AsyncRelay.Backend.csproj
```

打包可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name Wanxiang.AsyncRelay
```

`pack-mod` 会把 `Config.Lua` 和插件 DLL 组装到仓库根目录的
`artifacts/mods/Wanxiang.AsyncRelay/`。

插件项目的本地构建设置写在对应项目的 `Taiwu.Mod.props`。

## 项目结构

- `Config.Lua`：游戏读取的 mod 配置。
- `src/Frontend/`：前端插件项目。
- `src/Backend/`：后端插件项目。
