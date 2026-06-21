# 脚本执行共享模块

`src/Scripting/` 是前端插件和后端插件共用的受信 C# 脚本运行核心。它负责编译完整 C# 编译单元、检查入口
契约、在目标插件进程内尝试调用临时程序集入口，并产出脚本运行事实：入口未被调用时给出原因；入口已被调用
时给出入口返回值或入口异常。编译 warning 不进入 Agent 可见工具返回。

这个模块不承载 MCP 工具语义，也不直接定义 MessagePipe endpoint。跨进程请求与响应归 `src/Ipc/`，MCP
工具路由归 `src/McpServer/`，可访问的游戏 API、运行状态和线程边界归目标侧前端或后端模块。

脚本入口调用线程来自 IPC 请求中的 `entryThread`。共享运行器按该字段委托入口调用分派器；前端和后端模块负责把
`mainThread` 映射到各自的游戏主线程。

## 引用和程序集解析

Roslyn 编译需要 PE metadata。太吾 Mod 运行时可能用字节方式加载插件程序集，使已加载程序集没有可用的
`Assembly.Location`；脚本运行器因此不把 `Assembly.Location` 当作唯一引用来源。

编译引用按目标侧从这些来源收集：

- 目标侧插件部署目录，由前端或后端从游戏提供的相枢 Mod 目录派生，例如 `Plugins/Frontend` 或
  `Plugins/Backend`。
- 运行时的 `TRUSTED_PLATFORM_ASSEMBLIES`。
- 当前进程已加载且仍能定位到文件的程序集。

这些来源会以字节读取并交给 Roslyn 作为 metadata image。执行临时程序集时，运行器只在本次调用范围内
挂接程序集解析：先复用当前进程已加载的程序集，再从目标侧插件部署目录补载同名依赖。

目标侧插件部署目录不是手写 DLL 清单。可部署内容由入口项目、`Taiwu.Mod.Pack.proj`、各侧
`Taiwu.Mod.props` 和 `Wanxiang.Prelude`（万象引）的运行时依赖共同维护；本模块只消费打包结果。

脚本以完全信任方式运行，不提供沙箱。稳定读写游戏状态的 facade 由前端和后端模块按侧端能力补充。

## 入口契约

脚本内容是完整 C# 编译单元，不是 statements/expression 片段。协议和运行器不提供额外参数或预置
`using` 列表；脚本需要的 `using`、namespace、类型和返回值都由脚本自己声明。

运行器只按约定查找一个公开静态入口类型和入口方法：

```csharp
using Wanxiang.Xiangshu.Scripting;

public static class XiangshuScript
{
    public static object? Execute(XiangshuScriptGlobals globals)
    {
        return globals.Side;
    }
}
```

入口类型可以放在脚本自己的 namespace 下，但简单类型名必须是 `XiangshuScript`，且必须是
`public static` 非泛型 class。入口方法可以是 `Execute` 或 `ExecuteAsync`，必须是 public static 方法，
参数必须是 `Wanxiang.Xiangshu.Scripting.XiangshuScriptGlobals`，可以通过
`using Wanxiang.Xiangshu.Scripting;` 简写为 `XiangshuScriptGlobals`。同步返回值、`Task` 和 `Task<T>`
都会解析为入口返回值。

`entryThread` 只约束入口方法的调用线程，不改变脚本内部自行创建任务或目标侧异步 API 回调的线程语义。
