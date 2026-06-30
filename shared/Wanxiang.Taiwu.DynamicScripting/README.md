# Dynamic Scripting 共享模块

`Wanxiang.Taiwu.DynamicScripting` 是前后端插件可复用的受信 C# 动态脚本运行核心。它负责编译完整 C#
编译单元、检查调用方声明的入口契约、在当前插件进程中调用入口，并把结果整理为通用运行事实。

本模块只处理“如何编译并调用一个受信脚本入口”。工具语义、请求响应 DTO、宿主选择、线程分派策略和脚本可见 globals
对象由接入它的 Mod 适配层声明。

## 阅读对象

本文面向准备在某个 Mod 中接入动态脚本执行能力的维护者，以及维护本 shared 项目公共 API 的人。脚本作者应阅读各 Mod
自己的脚本入口说明，例如目标 Mod 的 `src/Scripting/README.md`。

## 本模块负责

- 收集 Roslyn metadata 引用，包括目标插件部署目录、调用方显式传入的程序集路径、TPA 和当前进程已加载程序集。
- 编译完整 C# 编译单元。
- 按 `DynamicScriptEntryContract` 查找唯一公开静态入口类型和入口方法。
- 通过 `IDynamicScriptEntryDispatcher` 把入口调用交给宿主线程分派策略。
- 将同步返回值、`Task` 和 `Task<T>` 结果序列化为缩进的 camelCase JSON。
- 把编译失败、引用缺失、入口异常和取消整理为 `DynamicScriptRunResult`。

## 调用方边界

- 脚本在目标插件进程内完全信任运行；沙箱或权限分级不属于本模块承诺。
- MCP 工具名称、参数描述、agent 可见响应 JSON 和 MessagePack/MessagePipe IPC 契约归调用方的 IPC/MCP 边界。
- 前端或后端游戏 API facade、`ScriptGlobals` 类型、入口类型名和命名空间归调用方的脚本适配层。
- 宿主选择、参数字典和其它调用方请求字段如果要暴露给脚本，应由调用方放进自己的 globals 类型。

调用方应保留自己的窄适配层，例如把 Mod 内部 IPC 请求拆成 `DynamicScriptRunRequest` 和调用方自己的 globals 对象，
再把 `DynamicScriptRunResult` 映射回该 Mod 的 IPC 响应。

## 接口说明

`DynamicScriptReferenceOptions` 描述引用发现环境：

- `ReferenceDirectories`：脚本编译时会读取这些目录顶层的 `*.dll` 作为 metadata reference；执行临时程序集时也会从这些目录解析依赖。
- `AssemblyReferencePaths`：脚本编译时显式加入的 DLL 文件。执行临时程序集时，运行器也会按程序集身份从这些路径解析依赖。

`DynamicScriptEntryContract` 描述某个 Mod 自己暴露给脚本作者的入口契约：

- `EntryTypeSimpleName`：入口类型简单名，例如 `GuanxiangtaiScript` 或 `XiangshuScript`。
- `ScriptGlobalsType`：入口方法唯一参数的精确类型。
- `ScriptGlobalsDisplayName`：诊断消息中显示的 globals 类型名。
- `ScriptAssemblyNamePrefix`：临时脚本程序集名称前缀。
- `AsyncEntryMethodName` / `SyncEntryMethodName`：入口方法名，默认是 `ExecuteAsync` 和 `Execute`。

`DynamicScriptRunRequest` 描述单次执行请求：

- `Script`：完整 C# 编译单元，不是片段。
- `EntryThread`：只约束入口方法调用线程。`Current` 表示调用方当前线程，`MainThread` 表示由宿主 dispatcher 定义的主线程。

`IDynamicScriptEntryDispatcher` 是宿主线程边界。运行器只把入口调用委托给它；调用方负责把 `MainThread`
映射到本宿主的主线程机制。

`DynamicScriptRunner` 是执行入口。构造时传入入口契约、引用选项和可选线程分派器；调用 `ExecuteAsync(request, globals, cancellationToken)`
后返回 `DynamicScriptRunResult`。`globals` 必须是 `DynamicScriptEntryContract.ScriptGlobalsType` 的实例，内部字段由调用方自己定义和填充。
`cancellationToken` 控制编译、入口分派和等待入口返回的 `Task`；脚本自身的长期工作需要通过调用方放入 globals 的取消令牌协作停止。

`DynamicScriptRunResult` 是通用结果联合：

- `DynamicScriptNotInvokedResult`：入口没有被调用。常见原因是引用缺失、编译失败、入口契约不满足、线程分派器在入口前拒绝调用或调用前取消。
- `DynamicScriptInvokedResult` + `DynamicScriptReturnValueOutcome`：入口已调用并返回；返回值在 `ReturnValueJson` 中。
- `DynamicScriptInvokedResult` + `DynamicScriptExceptionOutcome`：入口已调用，但入口或其返回的 `Task` 抛出异常或被取消。

## 最小接入示例

调用方先定义自己的脚本 globals 类型。这个类型是脚本作者能直接看到的 API 边界：

```csharp
namespace MyMod.Scripting;

public sealed class MyScriptGlobals(
    IReadOnlyDictionary<string, string> arguments,
    CancellationToken cancellationToken)
{
    public IReadOnlyDictionary<string, string> Arguments { get; } = arguments;

    public CancellationToken CancellationToken { get; } = cancellationToken;
}
```

再在 Mod 自己的脚本适配层中声明入口契约并创建 runner：

```csharp
using Wanxiang.Taiwu.DynamicScripting;

namespace MyMod.Scripting;

public sealed class MyScriptRunner
{
    private static readonly DynamicScriptEntryContract Contract = new(
        "MyScript",
        typeof(MyScriptGlobals),
        "MyMod.Scripting.MyScriptGlobals",
        "MyMod.DynamicScript");

    private readonly DynamicScriptRunner _runner;

    public MyScriptRunner(
        string pluginDirectory,
        IDynamicScriptEntryDispatcher entryDispatcher)
    {
        _runner = new DynamicScriptRunner(
            Contract,
            new DynamicScriptReferenceOptions(
                referenceDirectories: [pluginDirectory]),
            entryDispatcher);
    }

    public Task<DynamicScriptRunResult> ExecuteAsync(
        string script,
        IReadOnlyDictionary<string, string> arguments,
        DynamicScriptEntryThread entryThread,
        CancellationToken cancellationToken)
    {
        MyScriptGlobals globals = new(
            arguments,
            cancellationToken);

        return _runner.ExecuteAsync(
            new DynamicScriptRunRequest(script, entryThread),
            globals,
            cancellationToken);
    }
}
```

脚本作者提交的编译单元需要满足调用方声明的入口契约：

```csharp
using System.Threading.Tasks;
using MyMod.Scripting;

public static class MyScript
{
    public static Task<object?> ExecuteAsync(MyScriptGlobals globals)
    {
        return Task.FromResult<object?>(new
        {
            argumentCount = globals.Arguments.Count,
        });
    }
}
```

调用方最后把通用结果映射回自己的 IPC 或 MCP 响应。shared 层不替调用方决定 wire protocol：

```csharp
static object ToWireResponse(DynamicScriptRunResult result)
{
    return result switch
    {
        DynamicScriptNotInvokedResult notInvoked => new
        {
            kind = "notInvoked",
            reason = notInvoked.Reason,
            details = notInvoked.Details,
        },

        DynamicScriptInvokedResult
        {
            Outcome: DynamicScriptReturnValueOutcome returnValue,
        } => new
        {
            kind = "invoked",
            outcome = "returnValue",
            valueJson = returnValue.ReturnValueJson,
        },

        DynamicScriptInvokedResult
        {
            Outcome: DynamicScriptExceptionOutcome exception,
        } => new
        {
            kind = "invoked",
            outcome = "exception",
            message = exception.Message,
        },

        _ => throw new InvalidOperationException("Unhandled script result."),
    };
}
```

## 线程分派示例

没有传入 `IDynamicScriptEntryDispatcher` 时，运行器只支持 `DynamicScriptEntryThread.Current`；请求 `MainThread`
会在入口调用前失败并返回 `notInvoked`。

宿主需要支持主线程时，调用方实现自己的 dispatcher：

```csharp
using Wanxiang.Taiwu.DynamicScripting;

internal interface IHostMainThreadScheduler
{
    Task SwitchToMainThreadAsync(CancellationToken cancellationToken);
}

internal sealed class HostScriptEntryDispatcher(
    IHostMainThreadScheduler mainThreadScheduler) : IDynamicScriptEntryDispatcher
{
    public async Task<object?> InvokeAsync(
        Func<object?> invokeEntry,
        DynamicScriptEntryThread entryThread,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (entryThread)
        {
            case DynamicScriptEntryThread.Current:
                return invokeEntry();

            case DynamicScriptEntryThread.MainThread:
                await mainThreadScheduler.SwitchToMainThreadAsync(cancellationToken);
                return invokeEntry();

            default:
                throw new ArgumentOutOfRangeException(nameof(entryThread), entryThread, "Unsupported script entry thread.");
        }
    }
}
```

dispatcher 只负责入口方法的调用线程；入口返回的 `Task` 后续如何恢复线程，仍由脚本和宿主异步 API 自己决定。

## 引用解析规则

编译引用按这些来源合并并去重：

1. `TRUSTED_PLATFORM_ASSEMBLIES`。
2. `DynamicScriptReferenceOptions.AssemblyReferencePaths`。
3. `DynamicScriptReferenceOptions.ReferenceDirectories` 顶层 `*.dll`。
4. 当前 `AppDomain` 已加载且有可用 `Assembly.Location` 的程序集。
5. `DynamicScriptEntryContract.ScriptGlobalsType.Assembly`，这是必要引用；找不到时脚本不会被调用。

执行临时程序集时，运行器在本次调用范围内挂接 `AssemblyResolve`。解析顺序是：先复用当前进程已加载程序集，再按显式
`AssemblyReferencePaths` 匹配程序集身份，最后从 `ReferenceDirectories` 查找同名 DLL。

`ReferenceDirectories` 和 `AssemblyReferencePaths` 是宿主侧配置，不是 MCP 工具参数。宿主需要暴露额外可编译 API
时，由对应插件宿主解析出稳定路径后传给 runner。

## 维护约定

- shared 层只保存通用运行事实，不保存脚本内容、agent 会话、token 或宿主 IPC 地址。
- 脚本执行始终是完全信任边界；沙箱承诺应由更外层能力单独设计。
- Unity、GameData、MessagePipe 和 MCP 类型留在调用方或侧别适配项目。
- 新增脚本可见 facade 时，优先放在调用方的 globals 类型或宿主模块。
- 新增 wire protocol、工具参数或 agent 可见 JSON 时，放在调用方的 IPC/MCP 适配层。
