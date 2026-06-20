# shared

本仓库内部共享项目目录。

每个一级子目录是一个可被本仓库多个 mod 引用的内部 C# 项目。共享项目为插件项目提供内部库；部署共享项目 DLL
或其 runtime 依赖的动作，由引用它的前端或后端插件项目声明，具体 item 见 `mods/README.md`。

本目录 README 说明共享项目的共同边界。共享库自己的 API、事件选择、运行时部署建议和维护入口由
`shared/<ProjectName>/README.md` 维护；引用它的 mod 负责决定是否合并、复制或不部署该 DLL。

## 文档入口

| 目录 | 角色 | 继续阅读 |
| --- | --- | --- |
| `Wanxiang.Taiwu.Logging/` | 前后端插件共用的太吾游戏日志格式化适配层。 | `Wanxiang.Taiwu.Logging/README.md` |

新增或移除内部共享项目时，同步更新这张入口表。表中只保留选择信息；共享库 API、事件选择和部署建议留在项目
自己的 README 里。

新建内部共享项目：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- create-shared --name MyCompany.Taiwu.Shared
```

新建后，项目目录包含项目内 README 和一个 C# class library 项目。

创建命令生成共享项目的初始骨架。项目创建后，目标框架、Taiwu 引用、Publicizer 和部署建议以项目自己的 `.csproj`、
README，以及引用它的插件项目配置为准。

```text
shared/MyCompany.Taiwu.Shared/
  README.md
  MyCompany.Taiwu.Shared.csproj
```

共享项目的目标框架、Taiwu 引用和 Publicizer 配置写在项目自己的 `.csproj` 中。默认 `Shared`
和 `Frontend` 项目目标框架为 `netstandard2.1`，`Backend` 项目目标框架为 `net8.0`。纯共享抽象
或通用实现可以保持为普通 C# class library。

同一个共享项目如果会同时被前端和后端插件引用，并且依赖 `Taiwu.ModKit.References.*` 游戏引用包，
需要同时产出前端和后端运行时目标框架，例如 `netstandard2.1;net8.0`。这样前端插件消费
`netstandard2.1` 产物，后端插件消费 `net8.0` 产物，NuGet 会按目标框架选择对应的游戏引用资产。

需要访问游戏 API 时，再按实际代码需要添加 `Taiwu.ModKit.References.Frontend` 或
`Taiwu.ModKit.References.Backend` 等引用包。需要访问游戏 DLL 的非 public API 时，在项目自己的
`.csproj` 中显式添加 `Krafs.Publicizer` 引用、启用 `UsePublicizer`，并声明具体 `Publicize` 项。

`Taiwu.ModKit.References.*` 包的生成、分类和发布归组织内部
[`taiwu-modkit`](https://github.com/Wanxiang-Sanctum/taiwu-modkit) 仓库维护；共享项目通过稳定包 ID 和本仓库固定版本
引用这些包，DLL 清单以该内部仓库的工具配置为准。
