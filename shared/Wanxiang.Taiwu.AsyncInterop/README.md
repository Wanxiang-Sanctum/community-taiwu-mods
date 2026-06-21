# Wanxiang.Taiwu.AsyncInterop

前后端共用的太吾游戏异步 callback-awaitable 互操作原语。

`TaiwuAsyncCall` 接收一个会登记 `(offset, RawDataPool)` 回调的太吾异步 dispatch，并返回当前运行侧的 awaitable。
它把游戏 callback 结果读取成调用方需要的类型，同时把同步派发异常和 callback 读取异常落到同一个 awaitable 结果上。

同一源码面向两个运行侧：

- 前端 `netstandard2.1` 目标返回 `UniTask<TResult>`。
- 后端 `net8.0` 目标返回 `Task<TResult>`。

## 公开入口

`TaiwuAsyncCall.InvokeAsync<TResult>(...)` 接收一个 dispatch lambda。调用方在 lambda 里选择具体游戏接口，并把
`callback.Invoke` 传给该接口：

```csharp
TResult result = await TaiwuAsyncCall.InvokeAsync<TResult>(
    callback => DispatchSomeGameAsyncCall(
        arg0,
        arg1,
        callback.Invoke));
```

需要自定义读取逻辑时，使用带 `readResult` 的重载。默认重载通过 `SerializerHolder<TResult>` 从 `RawDataPool`
读取结果。

## 模块边界

本项目的稳定表面是 callback-awaitable 互操作。领域级便利 API、具体游戏接口选择、结果筛选、重试和业务降级策略，
应由拥有业务语义的端侧模块或更高层共享库组合本原语来表达。

条件编译只承载运行侧 awaitable 差异；具体 dispatch 仍由调用方在 lambda 中显式选择。

## 开发

从仓库根目录构建：

```powershell
dotnet build shared/Wanxiang.Taiwu.AsyncInterop/Wanxiang.Taiwu.AsyncInterop.csproj
```
