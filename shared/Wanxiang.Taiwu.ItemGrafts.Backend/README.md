# Wanxiang.Taiwu.ItemGrafts.Backend

行囊物品嫁接机制的后端观察项目。

后端观察服务负责观察游戏事实，并把宿主事件转发给同一 mod 前端的 `GraftSession`。真实宿主的创建由前端动作完成，
删除由游戏流程或上层业务触发。

## 边界

`BackendInventoryGrafts.Install(plugin)` 是后端安装入口。它从太吾插件实例读取本 mod id，安装本组件的后端观察服务，
并把后端宿主事件转发给前端会话。后端持有需要释放的注册资源、Harmony 补丁和宿主会话统计，因此提供
`Uninstall()`。

本组件的作用域限定为同一 mod 内的前后端协作。Harmony 观察补丁、宿主会话统计、后端事件流和跨端消息编码都是内部实现；
调用 `BackendInventoryGrafts.Install(plugin)` 后，前端会话通过 ItemGrafts 协议订阅宿主。

后端推送三类宿主事实：`Removed`、`LocationChanged`、`DataChanged`。`DataChanged` 表示宿主真实物品数据已变化；
耐久、精炼、淬毒等字段级解释归使用方查询真实物品数据后处理。

## 安装入口

后端插件初始化时安装观察服务：

```csharp
BackendInventoryGrafts.Install(this);
```

## 运行时行为

前端建立 `GraftSession` 时，后端按 `GraftHostId` 统计前端会话；同一个宿主被多个会话订阅时，只有最后一个
会话结束后才会停止观察。宿主事件会携带当前 `ItemKey`，因此宿主 `ModificationState` 改变后，前端会话可以更新
后续游戏调用使用的完整 key。

前端调用 `GraftSession.DisposeAsync()` 取消本次嫁接会话时，后端会减少该宿主的会话计数。宿主被游戏流程真实删除时，
后端会推送 `Removed` 事件并清掉该宿主的会话统计；删除行为归游戏流程或上层业务。

`BackendInventoryGrafts.Uninstall()` 会移除 ItemGrafts 的后端处理器、卸载 Harmony 补丁、清空宿主会话统计，
并停止向前端转发事件。
