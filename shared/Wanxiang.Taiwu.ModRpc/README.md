# Wanxiang.Taiwu.ModRpc

太吾 mod 专用 JSON RPC 封装。

本项目同时产出前端 `netstandard2.1` 和后端 `net8.0` 目标框架；公共 assembly 名和命名空间保持为
`Wanxiang.Taiwu.ModRpc`。调用方只使用 `RpcPeer` 和 `ModRpcException`。底层
`CallModMethod`、`ModDisplayEvent`、`SerializableModData`、response method 和 wire envelope 都是内部实现。

## 公开入口

`RpcPeer` 在前端和后端提供同一组概念入口：

- `Notify(modId, methodName, payloadJson)`：向对端发送单向通知，不等待返回。
- `InvokeAsync(modId, methodName, payloadJson, cancellationToken)`：调用对端 method 并等待 JSON 返回。前端目标返回
  `UniTask<string>`，后端目标返回 `Task<string>`。
- `Register(...)`：注册本端可被对端调用的 request handler。
- `Subscribe(...)`：注册本端可接收的 notification handler。

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
也不直接构造 envelope。

`InvokeAsync` 不内置超时策略；需要限制等待窗口时，由调用方传入 `CancellationToken`。request handler 抛出的异常或返回非法
JSON 会作为 `ModRpcException` 传回调用方。notification 没有响应通道；一个订阅 handler 抛异常不会阻断其他订阅 handler。

## Payload 契约

公开 payload 是 JSON 字符串。省略 payload 参数时发送 JSON literal `null`；显式传入 C# `null`、空白字符串或非法 JSON 会在
RPC 边界失败。request handler 收到的 payload 和返回值也必须是合法 JSON 文本。

`SerializableModData` 只作为前端到后端 transport 的内部载体，JSON 会写入内部 `json` 字段。缺少该字段的
`SerializableModData` 不是有效 ModRpc payload。后端到前端的 display event 必须携带 ModRpc protocol marker 和 JSON
payload；无关或损坏的 display event 会被忽略。

DTO 到 JSON 的转换可以由调用方协议模块负责，也可以使用 `RpcPeer` 的泛型重载。本项目不额外公开 serializer/codec 对象，
也不公开 `JsonSerializerSettings`。泛型重载使用 `TypeNameHandling.None`，只适合稳定 DTO；游戏运行时对象应由业务协议转成稳定字段。

`SerializableModData` 不作为公开 payload 类型支持。需要这类游戏原生载体时，调用方先转成稳定 JSON DTO。

## 注册生命周期

前端 display event handler 会在本端不再有 request handler 或 notification handler 时移除。

后端没有公开移除单个 `AddModMethod` 的 API。`RpcPeer.Register(...)` 和 `RpcPeer.Subscribe(...)` 返回的
`IDisposable` 会移除封装层 handler；底层已注册的游戏 method 壳会保留到 mod domain 生命周期结束。
