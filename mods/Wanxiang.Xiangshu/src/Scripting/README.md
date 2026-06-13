# 脚本执行共享模块

`src/Scripting/` 是前端插件和后端插件共用的本机 C# 脚本执行器。它不承载 MCP 工具语义，也不直接定义
MessagePipe endpoint；跨进程请求与响应仍由 `src/Ipc/` 定义。

当前执行器把受信脚本编译为临时内存程序集，并在目标插件进程内调用。它会引用当前进程已经加载且有物理
路径的程序集，因此脚本能访问该侧进程可见的公开游戏 API。脚本以完全信任方式运行；稳定读写游戏状态的
facade 由前端/后端模块按侧端能力补充。

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

入口类型可以放在脚本自己的 namespace 下，但简单类型名必须是 `XiangshuScript`。入口方法可以是
`Execute` 或 `ExecuteAsync`，参数必须是一个 `XiangshuScriptGlobals`。同步返回值、`Task` 和
`Task<T>` 都会按结果处理。

Roslyn 核心 DLL 会合并进相枢前后端入口；必要 `System.*` 辅助程序集由 `Wanxiang.FrontendRuntime`
前置 mod 部署。相枢项目保留编译期引用和脚本执行实现，但不在自己的前端或后端入口项目中复制这些同名
运行依赖。
