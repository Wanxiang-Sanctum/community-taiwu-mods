# 脚本执行适配模块

`src/Scripting/` 是前端插件和后端插件共用的相枢脚本入口适配层。它声明相枢自己的入口契约，并把完整 C# 编译单元编译、
引用解析、入口调用和返回值整理委托给 `shared/Wanxiang.Taiwu.DynamicScripting`。编译 warning 不进入 Agent 可见工具返回。

跨进程请求与响应归 `src/Ipc/`，MCP 工具路由归 `src/McpServer/`，可访问的游戏 API、运行状态和线程边界归目标侧
前端或后端模块。本模块只在这些边界之间声明相枢脚本入口契约。

脚本入口调用线程来自 IPC 请求中的 `entryThread`。共享运行器按该字段委托入口调用分派器；前端和后端模块负责把
`mainThread` 映射到各自的游戏主线程。

## 引用和程序集解析

Roslyn 编译需要 PE metadata。太吾 Mod 运行时可能用字节方式加载插件程序集，使已加载程序集没有可用的
`Assembly.Location`；脚本运行器因此不把 `Assembly.Location` 当作唯一引用来源。

编译引用按目标侧从这些来源收集：

- 目标侧插件部署目录，由前端或后端从游戏提供的相枢 Mod 目录派生，例如 `Plugins/Frontend` 或
  `Plugins/Backend`。
- 宿主侧显式传给脚本运行器的程序集引用路径。前端用这条边界把运行时已加载的 UniTask 程序集解析为编译引用，
  使脚本可以显式 `using Cysharp.Threading.Tasks;` 并在 `Task<object?>` 入口内部 await UniTask API。
- 运行时的 `TRUSTED_PLATFORM_ASSEMBLIES`。
- 当前进程已加载且仍能通过 `Assembly.Location` 定位到文件的程序集。

这些来源会以字节读取并交给 Roslyn 作为 metadata image。执行临时程序集时，运行器只在本次调用范围内
挂接程序集解析：先复用当前进程已加载的程序集，再按宿主显式程序集引用路径匹配程序集身份，最后从目标侧插件部署目录按程序集身份补载依赖。

显式程序集引用路径是目标侧宿主配置，不是 MCP 工具参数，也不是预置命名空间。宿主可以从运行时类型取得程序集身份，
再按目标侧插件加载目录查找可读 DLL；脚本仍然自己声明需要的 `using`，入口异步契约仍是 `Task` 或 `Task<T>`。

目标侧插件部署目录由宿主和打包流程提供，不在脚本模块维护手写 DLL 清单；本模块只消费宿主传入的目录和引用路径。

脚本以完全信任方式运行，不提供沙箱。稳定读写游戏状态的 facade 由前端和后端模块按侧端能力补充。

## 入口契约

运行器编译完整 C# 编译单元。脚本入口只接收 `XiangshuScriptGlobals`；脚本需要的 `using`、namespace、类型
和返回值都由脚本自己声明。

运行器只按约定查找一个公开静态入口类型和入口方法：

```csharp
using System.Threading.Tasks;
using Wanxiang.Xiangshu.Scripting;

public static class XiangshuScript
{
    public static Task<object?> ExecuteAsync(XiangshuScriptGlobals globals)
    {
        return Task.FromResult<object?>(new { side = globals.Side });
    }
}
```

入口类型可以放在脚本自己的 namespace 下，但简单类型名必须是 `XiangshuScript`，且必须是
`public static` 非泛型 class。入口方法可以是 `Execute` 或 `ExecuteAsync`，必须是 public static 方法，
参数必须是 `Wanxiang.Xiangshu.Scripting.XiangshuScriptGlobals`，可以通过
`using Wanxiang.Xiangshu.Scripting;` 简写为 `XiangshuScriptGlobals`。同步返回值、`Task` 和 `Task<T>`
都会解析为入口返回值；推荐需要异步时让入口返回 `Task<object?>`。UniTask 适合作为入口内部 await 的
前端 API；脚本入口的宿主异步契约仍是 `Task` 或 `Task<T>`。

入口返回值会整理为缩进的 camelCase JSON。需要返回游戏运行时对象时，脚本应先转成稳定字段对象，而不是直接
暴露运行时类型。

`entryThread` 只约束入口方法的调用线程，不改变脚本内部自行创建任务或目标侧异步 API 回调的线程语义。
