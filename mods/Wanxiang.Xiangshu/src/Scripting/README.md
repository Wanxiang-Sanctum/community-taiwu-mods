# 脚本执行适配模块

`src/Scripting/` 是前端插件和后端插件共用的相枢脚本入口适配层。它声明相枢自己的入口契约，并把完整 C# 编译单元编译、
引用解析、入口调用和返回值整理委托给 `shared/Wanxiang.Taiwu.DynamicScripting`。编译 warning 不进入 Agent 可见工具返回。

脚本可见的 `XiangshuScriptGlobals` 定义在相邻的 `src/Scripting.Contracts/` 项目中，并作为独立 DLL 部署。
`src/Scripting/` 只负责运行器适配、IPC 结果映射和入口契约声明；引用路径由目标侧宿主通过
`DynamicScriptReferenceOptions` 提供。

跨进程请求与响应归 `src/Ipc/`，MCP 工具路由归 `src/McpServer/`，可访问的游戏 API、运行状态和线程边界归目标侧
前端或后端模块。本模块只在这些边界之间声明相枢脚本入口契约。

脚本入口调用线程来自 IPC 请求中的 `entryThread`。共享运行器按该字段委托入口调用分派器；前端和后端模块负责把
`mainThread` 映射到各自的游戏主线程。

## 引用和程序集解析

Roslyn 编译需要 PE metadata。太吾 Mod 运行时可能用字节方式加载插件程序集，使已加载程序集没有可用的
`Assembly.Location`；脚本运行器因此不把 `Assembly.Location` 当作唯一引用来源。

编译引用按目标侧从这些来源收集：

- 宿主侧显式传给脚本运行器的程序集引用路径。相枢宿主会加入 `Wanxiang.Xiangshu.Scripting.Contracts.dll`，
  这是脚本入口参数类型所在的窄契约程序集。
- 前端脚本宿主通过 `FrontendScriptReferences.CreateOptions` 固定加入运行时 UniTask 编译引用，
  使脚本可以显式 `using Cysharp.Threading.Tasks;` 并在 `Task<object?>` 入口内部 await UniTask API。
- 运行时的 `TRUSTED_PLATFORM_ASSEMBLIES`。
- 当前进程已加载且仍能通过 `Assembly.Location` 定位到文件的程序集。

这些来源会以字节读取并交给 Roslyn 作为 metadata image。执行临时程序集时，运行器只在本次调用范围内
挂接程序集解析：先复用当前进程已加载且完整身份相同的程序集，再按宿主显式程序集引用路径精确匹配程序集身份。

显式程序集引用路径是目标侧宿主配置，不是 MCP 工具参数，也不是预置命名空间。后端宿主用
`DynamicScriptAssemblyReferenceResolver` 解析必需的脚本契约程序集；前端宿主用 `FrontendScriptReferences.CreateOptions`
创建引用选项，固定加入脚本契约程序集和 UniTask 编译引用。脚本仍然自己声明需要的 `using`，入口异步契约仍是 `Task` 或
`Task<T>`。

目标侧插件部署目录由宿主和打包流程提供，但不会被整体扫描为脚本引用；本模块只消费宿主解析出的精确程序集路径。

脚本以完全信任方式运行，不提供沙箱。稳定读写游戏状态的 facade 由前端和后端模块按侧端能力补充。

## 入口契约

运行器编译完整 C# 编译单元。脚本入口只接收 `XiangshuScriptGlobals`，入口类型完整名必须是
`Wanxiang.Xiangshu.Scripting.XiangshuScript`。脚本可以在入口外自行声明需要的 `using`、辅助类型和返回值结构。

运行器只按约定查找一个公开静态入口类型和入口方法：

```csharp
using System.Threading.Tasks;

namespace Wanxiang.Xiangshu.Scripting;

public static class XiangshuScript
{
    public static Task<object?> ExecuteAsync(XiangshuScriptGlobals globals)
    {
        return Task.FromResult<object?>(new { side = globals.Side });
    }
}
```

入口类型必须是 `public static` 非泛型 class。入口方法可以是 `Execute` 或 `ExecuteAsync`，必须是
public static 方法，参数必须是 `Wanxiang.Xiangshu.Scripting.XiangshuScriptGlobals`。同步返回值、`Task` 和
`Task<T>` 都会解析为入口返回值；推荐需要异步时让入口返回 `Task<object?>`。UniTask 适合作为入口内部
await 的前端 API；脚本入口的宿主异步契约仍是 `Task` 或 `Task<T>`。

入口返回值会整理为缩进的 camelCase JSON。需要返回游戏运行时对象时，脚本应先转成稳定字段对象，而不是直接
暴露运行时类型。

`entryThread` 只约束入口方法的调用线程，不改变脚本内部自行创建任务或目标侧异步 API 回调的线程语义。
