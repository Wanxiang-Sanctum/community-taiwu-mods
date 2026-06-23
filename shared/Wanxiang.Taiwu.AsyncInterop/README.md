# Wanxiang.Taiwu.AsyncInterop

前后端共用的太吾游戏异步回调与可等待对象互操作原语。

`TaiwuAsyncCall` 接收一个会登记 `(offset, RawDataPool)` 回调的太吾异步派发动作，并返回当前运行侧的可等待对象。
它把游戏回调结果读取成调用方需要的类型；同步派发失败和回调结果读取失败都会反映到返回的可等待对象。

同一源码面向两个运行侧：

- 前端 `netstandard2.1` 目标返回 `UniTask<TResult>`。
- 后端 `net8.0` 目标返回 `Task<TResult>`。

## 公开入口

`TaiwuAsyncCall.InvokeAsync<TResult>(...)` 接收一个派发 lambda。调用方在 lambda 里选择具体游戏接口，并把
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

本项目的稳定表面是回调与可等待对象互操作。具体游戏接口选择、结果筛选、重试和业务降级策略属于调用方或更高层共享库；
这里不把领域语义包装进通用原语。

条件编译只承载运行侧可等待类型差异；具体派发动作仍由调用方在 lambda 中显式选择。

## 开发

从仓库根目录构建：

```powershell
dotnet build shared/Wanxiang.Taiwu.AsyncInterop/Wanxiang.Taiwu.AsyncInterop.csproj
```
