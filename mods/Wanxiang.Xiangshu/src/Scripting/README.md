# 脚本执行共享模块

`src/Scripting/` 是前端插件和后端插件共用的本机 C# 脚本执行器。它不承载 MCP 工具语义，也不直接定义
MessagePipe endpoint；跨进程请求与响应仍由 `src/Ipc/` 定义。

执行器把受信脚本编译为临时内存程序集，并在目标插件进程内调用。编译时会显式引用定义
`XiangshuScriptGlobals` 的 `Wanxiang.Xiangshu.Scripting` 程序集，并补充当前进程已经加载且有物理路径的
程序集，因此脚本能访问入口契约类型和该侧进程可见的公开游戏 API。脚本以完全信任方式运行；稳定
读写游戏状态的 facade 由前端/后端模块按侧端能力补充。

脚本内容是完整 C# 编译单元，不是 statements/expression 片段。协议和 runner 不提供额外参数或预置
`using` 列表；脚本需要的 `using`、namespace、类型和返回值都由脚本自己声明。

runner 只按约定查找一个公开静态入口类型和入口方法：

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
都会按结果处理。

脚本编译所需运行时由入口项目和 `Wanxiang.Prelude`（万象引）按侧端部署。本模块只维护脚本形态、编译与
调用约定，不维护可部署 DLL 清单。
