# Wanxiang.Taiwu.ItemGrafts.Backend

行囊物品嫁接机制的后端观察项目。

后端观察服务不创建真实宿主，也不删除宿主物品；它只观察游戏事实，并把宿主事件转发给同一 mod 前端的 `GraftSession`。
真实宿主的创建由前端动作完成，删除仍由游戏流程或上层业务触发。

## 边界

`BackendInventoryGrafts.Install(plugin)` 是唯一的后端安装入口。它从太吾插件实例读取本 mod id，安装本组件的后端观察服务，
并把后端宿主事件转发给前端会话。后端持有需要释放的注册资源、Harmony 补丁和宿主会话统计，因此提供
`Uninstall()`；前端没有对应的全局卸载入口，前端嫁接会话由 `GraftSession.DisposeAsync()` 逐个结束。

本组件只面向同一 mod 内的前后端协作，不提供跨 mod 通讯入口。Harmony 观察补丁、宿主会话统计、后端事件流和跨端消息编码
都是内部实现。调用方不订阅后端观察事件，也不直接登记宿主或安装补丁；调用 `BackendInventoryGrafts.Install(plugin)` 后，
前端会话可以订阅宿主。

后端推送三类宿主事实：`Removed`、`LocationChanged`、`DataChanged`。`DataChanged` 只表示宿主真实物品数据已变化，
不细分为耐久、精炼、淬毒等字段；前端收到后按使用方自己的展示和业务需要重新查询。

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
后端会推送 `Removed` 事件并清掉该宿主的会话统计；删除物品本身不属于 ItemGrafts 的消息协议。

`BackendInventoryGrafts.Uninstall()` 会移除 ItemGrafts 的后端处理器、卸载 Harmony 补丁、清空宿主会话统计并停止向前端转发事件。
