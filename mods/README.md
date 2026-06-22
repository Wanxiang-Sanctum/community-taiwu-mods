# mods

实际 Mod 源码和组包规则目录。

本文面向仓库维护者。每个一级子目录是一个本仓库拥有的独立 Mod；本文维护一级 Mod 目录索引、Mod 目录约定，以及所有
Mod 共同遵守的组包、插件入口、引用和部署规则。

具体 Mod 的 `README.md` 面向外部技术玩家；源码模块、内部设计和项目内维护入口由对应
`mods/<ModName>/DEVELOPMENT.md`、`mods/<ModName>/docs/` 或源码子目录 README 维护。

## 目录约定

`mods/` 下的一级子目录就是实际 Mod 边界。目录名同时用于 `pack-mod --name <ModName>`、发布 tag 中的
`mods/<ModName>/v<Version>` 约定，以及可部署目录 `artifacts/mods/<ModName>/`。

## 文档入口

| 目录 | 角色 | 玩家说明 | 维护入口 |
| --- | --- | --- | --- |
| `Wanxiang.Prelude/` | 提供共享运行时和插件依赖加载规则的前置 Mod。 | `Wanxiang.Prelude/README.md` | `Wanxiang.Prelude/DEVELOPMENT.md` |
| `Wanxiang.Xiangshu/` | 太吾绘卷本机 Agent 对话 Mod。 | `Wanxiang.Xiangshu/README.md` | `Wanxiang.Xiangshu/DEVELOPMENT.md` |

这张表是 `mods/` 一级目录的索引，只保留选择信息和稳定入口。玩家说明留在对应 `README.md`，源码模块说明留在对应
`DEVELOPMENT.md`、`docs/` 或源码子目录 README。新增、移除或重命名一级 Mod 目录时，同步更新这张表；共同组包规则或
目录约定变化时，再修改本文其它部分。

新建 Mod：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- create-mod --name MyMod
```

新建后，Mod 目录包含游戏读取的 `Config.Lua`、项目内 README，以及前端和后端两个插件项目。
太吾游戏读取 `Config.Lua` 以及同步 Steam Workshop 字段的通用语义见仓库级文档
[`docs/taiwu-mod-steam-publishing-boundary.md`](../docs/taiwu-mod-steam-publishing-boundary.md)。

创建命令生成新 Mod 的初始骨架。项目创建后，真实包内容由该 mod 的 `Taiwu.Mod.Pack.proj`、插件项目文件和项目旁
`Taiwu.Mod.props` 维护；`templates/` 只作为新项目起点。新增实际 Mod 时，`README.md` 保持玩家说明视角，
`DEVELOPMENT.md` 承接源码维护入口。

```text
mods/MyMod/
  Config.Lua
  Taiwu.Mod.Pack.proj
  README.md
  DEVELOPMENT.md
  src/
    Frontend/
    Backend/
```

每个插件项目通过项目旁的 `Taiwu.Mod.props` 标记端侧。前端项目默认目标框架为
`netstandard2.1`，后端项目默认目标框架为 `net8.0`；这两个默认值由
`mods/Directory.Build.props` 根据端侧设置。

构建插件项目时可以直接使用 `dotnet build`。普通构建只负责 SDK 默认的 `bin/` 和 `obj/` 输出，
不解析包产物、不组装 `artifacts/mods/`，也不执行依赖合并。

打包可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name MyMod
```

`pack-mod` 默认使用 `Release` 运行 `mods/MyMod/Taiwu.Mod.Pack.proj`，并把该组包入口
声明的文件、目录和项目产物组装到 `artifacts/mods/MyMod/`。

## 组包声明

每个 Mod 的 `Taiwu.Mod.Pack.proj` 是可部署目录的组包入口。它只描述最终目录由哪些文件、目录和
项目组成，不给项目额外标记类型；太吾插件、共享 DLL、发布目录和普通静态文件都走同一条
组合路径。

```xml
<Project>
  <Import
    Project="$([MSBuild]::GetPathOfFileAbove('Taiwu.Mods.Paths.props', '$(MSBuildThisFileDirectory)'))"
  />
  <Import Project="$(TaiwuModsModsDir)Taiwu.Mod.Pack.targets" />

  <ItemGroup>
    <TaiwuModPackFile Include="Config.Lua" PackagePath="Config.Lua" />
    <TaiwuModPackProject Include="src/Frontend/MyMod.Frontend.csproj" />
    <TaiwuModPackProject Include="src/Backend/MyMod.Backend.csproj" />
  </ItemGroup>
</Project>
```

在组包入口中，`TaiwuModPackFile` 复制单个文件，`TaiwuModPackDirectory` 复制目录，
`TaiwuModPackProject` 引入一个参与组包的项目。包内路径写在 `PackagePath` 元数据中，必须是相对路径。

被 `TaiwuModPackProject` 引入的项目通过项目级包产物进入最终目录。`mods/Directory.Build.targets`
已经为 `mods/` 下的普通 SDK 项目导入默认项目组包目标；前端和后端插件项目还会自动把入口 DLL
声明为 `Plugins/<TargetFileName>`。模板生成的前后端项目通常只需要在 `Taiwu.Mod.Pack.proj`
中被引用，不需要手写入口程序集声明。

项目自身需要额外输出文件或目录时，在项目文件或项目旁的 `Taiwu.Mod.props` 中声明：

```xml
<ItemGroup>
  <TaiwuModPackFile Include="$(TargetDir)MyMod.Ipc.dll" PackagePath="Plugins/MyMod.Ipc.dll" />
</ItemGroup>
```

项目级可用声明包括：

- `TaiwuModPackFile`：复制单个文件。
- `TaiwuModPackDirectory`：复制目录。
- `TaiwuModPackEntry`：入口程序集。只有项目需要自行声明入口 DLL 并参与依赖合并时才直接使用。

`TaiwuModPackProject` 只用于 Mod 的组包入口，不在项目级继续嵌套。

需要把某个项目的 `dotnet publish` 输出目录整体放进包内时，在该项目中导入发布目录组包目标：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <TaiwuModPublishPackagePath>Tools/Worker</TaiwuModPublishPackagePath>
  </PropertyGroup>

  <Import Project="$(TaiwuModsModsDir)Taiwu.Mod.PublishDirectory.Pack.targets" />
</Project>
```

然后在 Mod 的 `Taiwu.Mod.Pack.proj` 中加入：

```xml
<ItemGroup>
  <TaiwuModPackProject Include="src/Worker/MyMod.Worker.csproj" />
</ItemGroup>
```

`pack-mod` 会先运行该项目的 `Publish` target，再把 `$(PublishDir)` 复制到 `Tools/Worker/`。
没有显式设置 `TaiwuModPublishPackagePath` 时，发布目录默认进入 `Processes/<ProjectName>/`。
项目可以用普通 .NET publish 属性控制是否 self-contained、single-file、RID 等发布细节。

## Taiwu 引用和 Publicizer

插件项目默认引用 `Taiwu.ModKit.References.Plugin`。需要访问更宽的游戏 API 时，再按实际代码需要
添加 `Taiwu.ModKit.References.Frontend` 或 `Taiwu.ModKit.References.Backend` 等引用包。

这些 `Taiwu.ModKit.References.*` 包由组织内部
[`taiwu-modkit`](https://github.com/Wanxiang-Sanctum/taiwu-modkit) 仓库的引用包工具生成和发布；包拆分原则、DLL
选择和发布目标归该仓库的工具配置维护。本公开仓库通过稳定包 ID 选择需要引用的包，并在仓库根
`Directory.Packages.props` 固定版本。

插件项目默认具备编译期 Publicizer 支持，但不会自动公开化游戏 DLL。需要在编译期访问游戏 DLL
的非 public API 时，在 `Taiwu.Mod.props` 中声明具体 `Publicize` 项；只声明实际需要的程序集、类型或成员。

```xml
<ItemGroup>
  <Publicize
    Include="Assembly-CSharp"
    IncludeCompilerGeneratedMembers="false"
    IncludeVirtualMembers="false"
  />
</ItemGroup>
```

前端通常从 `Assembly-CSharp` 开始，后端通常从 `GameData` 开始；如果只需要具体类型或成员，优先写
更窄的 `Publicize Include`。Publicizer 运行时策略由仓库按端侧固定选择，不作为普通 Mod 配置入口。

需要关闭默认 Publicizer 支持时，可以在 `Taiwu.Mod.props` 中设置：

```xml
<PropertyGroup>
  <UsePublicizer>false</UsePublicizer>
</PropertyGroup>
```

## 插件入口和依赖部署

太吾读取 `Config.Lua` 中的 `FrontendPlugins` 和 `BackendPlugins`，并从 Mod 的 `Plugins/`
目录加载这些插件入口 DLL。列表项是相对 `Plugins/` 的入口路径，可以是文件名，也可以包含子目录；
独立依赖 DLL 同样部署到 `Plugins/` 下。

前端和后端插件项目会自动把自身入口 DLL 声明为 `Plugins/<TargetFileName>`。额外依赖需要在插件项目旁
的 `Taiwu.Mod.props` 或项目文件中显式声明。普通 `dotnet build` 负责生成项目常规输出；`pack-mod`
在构建后读取项目包产物组装最终包。

需要把入口和复制依赖部署到 `Plugins/` 下的子目录时，在插件项目旁的 `Taiwu.Mod.props` 中设置：

```xml
<PropertyGroup>
  <TaiwuModPluginSubdirectory>Frontend</TaiwuModPluginSubdirectory>
</PropertyGroup>
```

`TaiwuModPluginSubdirectory` 是相对 `Plugins/` 的子目录，例如 `Frontend` 或
`Frontend/Tools`，不要包含 `Plugins/` 前缀或首尾斜杠。留空时入口和复制依赖直接部署到 `Plugins/`。
设置后，在 `Config.Lua` 中使用相对 `Plugins/` 的入口路径，例如
`Frontend/MyMod.Frontend.dll`。这适合同一个 Mod 的前端和后端入口需要分别携带同名 DLL、且运行时必须按入口
目录优先解析依赖的场景。原生加载器不会完整处理这种依赖解析需求；需要这条加载规则的 Mod 应声明
`Wanxiang.Prelude`（万象引）为前置依赖。

依赖部署有两种动作。需要作为独立文件复制到 `Plugins/` 时，声明：

```xml
<ItemGroup>
  <TaiwuModCopyDependency Include="Other.Assembly.dll" />
</ItemGroup>
```

需要合并到入口插件 DLL 时，声明：

```xml
<ItemGroup>
  <TaiwuModMergeDependency Include="Your.Assembly.dll" />
</ItemGroup>
```

每个依赖选择一种动作。同一个 DLL 同时写进 `TaiwuModMergeDependency` 和
`TaiwuModCopyDependency` 会报错；复制依赖默认写入 `Plugins/<DLL 文件名>`，入口 DLL 部署到
`Plugins/` 下的子目录时，复制依赖跟随入口 DLL 的目录。这两个依赖声明只表达太吾
插件入口的 DLL 处理方式；非插件项目的运行时依赖应放在项目自己的发布目录中。

`Include` 只写 DLL 文件名。`pack-mod` 不读取 NuGet 缓存路径或任意项目输出路径，而是在入口项目
本次构建后，从进入该项目输出目录的 DLL 中按文件名匹配，再执行复制或合并。

需要随 Mod 部署的依赖，要先通过项目自身的 `ProjectReference`、`PackageReference` 等标准引用进入
入口项目输出目录，再用 `TaiwuModMergeDependency` 或 `TaiwuModCopyDependency` 声明打包动作。
游戏或运行时已经提供的 DLL 作为外部运行时依赖处理。

被合并的依赖不会内部化，也不会重命名类型。太吾只按 `Config.Lua` 发现并加载入口 DLL；入口 DLL 是 Mod
的运行时边界。保留合并依赖的公开可见性可以避免破坏反射、序列化、IPC/RPC、脚本和插件加载桥等按完整类型名建立的契约。

前后端共同引用的内部共享项目如果要随入口一起部署，由前端和后端入口项目分别声明需要合并或复制的
DLL。这样前后端各自生成自己的最终入口 DLL。
