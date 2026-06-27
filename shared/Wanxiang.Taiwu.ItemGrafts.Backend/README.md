# Wanxiang.Taiwu.ItemGrafts.Backend

物品嫁接机制的后端创建与观察项目。

后端服务负责创建真实宿主、观察游戏事实，并把宿主事件转发给同一 Mod 前端的 `GraftSession`。真实宿主的创建由前端动作发起，
后端按请求的游戏 owner 写入对应游戏集合，并返回宿主 `ItemKey`；删除由游戏流程或上层业务触发。

## 边界

`BackendItemGrafts.Install(plugin)` 是后端安装入口。它从太吾插件实例读取本 Mod ID，安装本组件的宿主创建处理器和
后端观察服务，并把后端宿主事件转发给前端会话。后端持有需要释放的注册资源、Harmony 观察点和宿主会话统计，因此提供
`Uninstall()`。

本组件的作用域限定为同一 Mod 内的前后端协作。宿主创建处理器、Harmony 观察点、宿主会话统计、后端事件流和跨端消息编码
都是内部实现；调用 `BackendItemGrafts.Install(plugin)` 后，前端动作通过 ItemGrafts 协议请求创建和订阅宿主。

后端推送三类宿主事实：`Removed`、`OwnerChanged`、`DataChanged`。`DataChanged` 表示宿主真实物品数据已变化；
耐久、精炼、淬毒等字段级解释归使用方查询真实物品数据后处理。

## 安装入口

后端插件初始化时安装后端服务：

```csharp
BackendItemGrafts.Install(this);
```

## 运行时行为

前端创建新宿主时，后端按请求的 `GraftHostOwnerKey` 创建数量为 1 的非堆叠真实物品，并调用游戏已有集合 API 写入目标 owner。
公开协议只传输游戏 owner，不维护嫁接侧创建目标枚举；后端只在能找到对应游戏集合写入 API 时创建。当前实现覆盖角色行囊，以及太吾村
`Warehouse`、`Treasury`、`Stock`、`Trough` 这些游戏库存 owner。其它 owner 变化仍会被观察并上报；创建请求若没有对应
集合写入路径会失败，不会用裸 `SetOwner` 伪造创建成功。

前端建立 `GraftSession` 时，后端按 `GraftHostId` 统计前端会话；同一个宿主被多个会话订阅时，只有最后一个
会话结束后才会停止观察。宿主事件会携带当前 `ItemKey`，因此宿主 `ModificationState` 改变后，前端会话可以更新
后续游戏调用使用的完整 key。

前端调用 `GraftSession.DisposeAsync()` 取消本次嫁接会话时，后端会减少该宿主的会话计数。宿主 owner 变化通过
`ItemBase.SetOwner`、`RemoveOwner` 和 `ResetOwner` 观察并转发为 `OwnerChanged`。宿主被游戏流程真实删除时，
后端会推送 `Removed` 事件并清掉该宿主的会话统计；删除行为归游戏流程或上层业务。

`BackendItemGrafts.Uninstall()` 会移除 ItemGrafts 的创建和订阅处理器、卸载 Harmony 观察点、清空宿主会话统计，
并停止向前端转发事件。
