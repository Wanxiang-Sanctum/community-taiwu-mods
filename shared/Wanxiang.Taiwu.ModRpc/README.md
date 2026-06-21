# Wanxiang.Taiwu.ModRpc

太吾 mod 内部前后端 JSON RPC 封装。

本项目同时产出前端 `netstandard2.1` 和后端 `net8.0` 目标框架；公共 assembly 名和命名空间保持为
`Wanxiang.Taiwu.ModRpc`。实际 mod 包会合并并内部化本 DLL，因此一个 `Wanxiang.Taiwu.ModRpc` 副本只服务于一个
mod 的前后端通信，不提供跨 mod 路由。

调用方只使用 `RpcPeer` 和 `ModRpcException`。底层 `CallModMethod`、`ModDisplayEvent`、`SerializableModData`、
response method 和内部 envelope 都是实现细节。

## 公开入口

前端和后端启动时都先绑定本 mod id：

```csharp
RpcPeer.Bind("My.Mod.Id");
```

`Bind(...)` 可用同一个 mod id 重复调用；如果同一个内部化副本试图绑定到不同 mod id，会失败。绑定之后，
`RpcPeer` 在前端和后端提供同一组概念入口；这些入口只面向本 mod 对端，不接收目标 mod id：

- `Notify(methodName, payloadJson)`：向本 mod 对端发送单向通知，不等待返回。
- `InvokeAsync(methodName, payloadJson, cancellationToken)`：调用本 mod 对端 method 并等待 JSON 返回。前端返回
  `UniTask<string>`，后端返回 `Task<string>`。
- `Register(...)`：注册本端可被本 mod 对端调用的 request handler。
- `Subscribe(...)`：注册本端可接收的 notification handler。

`Register(...)` 只处理 `InvokeAsync(...)` 发出的 request；`Subscribe(...)` 只处理 `Notify(...)` 发出的 notification。

这些入口同时提供泛型 JSON 重载：

- `Notify<TPayload>(...)`
- `InvokeAsync<TResponse>(...)`
- `InvokeAsync<TRequest, TResponse>(...)`
- `Register<TRequest, TResponse>(...)`
- `Subscribe<TPayload>(...)`

泛型重载只是把 DTO 通过 Newtonsoft.Json 转成 JSON 字符串，再走同一套 JSON payload RPC；不会引入第二套协议。
后端 `Register` / `Subscribe` 的 handler 会额外接收游戏 `DataContext`，用于处理后端 domain callback。

## 运行模型

游戏给 mod 暴露的底层通信能力并不完全对称：

- 前端到后端调用使用游戏原生 `CallModMethod...`。
- 后端到前端调用由封装层用 `ModDisplayEvent`、request id 和内部 response method 补齐。

`RpcPeer` 把这两种传输收敛成 `Notify`、`InvokeAsync`、`Register` 和 `Subscribe`。调用方不选择 transport，
也不直接构造 envelope。底层游戏 API 仍以 mod id 标识本 mod；该值只在 `Bind(...)` 时进入封装层，之后不再作为调用参数出现。

`InvokeAsync` 不内置超时策略；需要限制等待窗口时，由调用方传入 `CancellationToken`。request handler 抛出的异常或返回非法
JSON 会作为 `ModRpcException` 传回调用方。notification 没有响应通道；一个订阅 handler 抛异常不会阻断其他订阅 handler。

## Payload 契约

公开传输 payload 是 JSON 字符串。省略 payload 参数时发送 JSON literal `null`。使用 `payloadJson` 字符串重载时，
显式传入 C# `null`、空白字符串或非法 JSON 会在 RPC 边界失败；request handler 收到的 payload 和返回值也必须是合法
JSON 文本。

`SerializableModData` 只作为前端到后端 transport 的内部载体，JSON 会写入内部 `json` 字段。缺少该字段的
`SerializableModData` 不是有效 ModRpc payload。后端到前端的 display event 必须携带 ModRpc protocol marker 和 JSON
payload；无关或损坏的 display event 会被忽略。

DTO 到 JSON 的转换可以由调用方协议模块负责，也可以使用 `RpcPeer` 的泛型重载。泛型 payload 为 C# `null` 时会按
JSON literal `null` 发送；泛型 response 只有在目标类型可接收 null 时才接受 JSON null。本项目不额外公开
serializer/codec 对象，也不公开 `JsonSerializerSettings`。泛型重载使用 `TypeNameHandling.None`，只适合稳定 DTO；
游戏运行时对象应由业务协议转成稳定字段。

`SerializableModData` 不作为公开 payload 类型支持。需要这类游戏原生载体时，调用方先转成稳定 JSON DTO。

## 注册生命周期

前端 `Bind(...)` 会注册本 mod 的 display event handler，并把不属于 ModRpc 协议的 display event 忽略。

后端 `RpcPeer.Register(...)` 和 `RpcPeer.Subscribe(...)` 返回的 `IDisposable` 只移除封装层 handler；底层已注册的
游戏 method 壳会保留到 mod domain 生命周期结束。
