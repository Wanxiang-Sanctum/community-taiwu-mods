# 寄身 IPC 协议

`src/Ipc/ItemGrafts/` 是相枢寄身功能的跨进程协议子模块。它只定义 MessagePipe DTO；请求处理器放在前端和后端各自的
`Ipc/ItemGrafts/`，主动发起对端请求的代码归对应运行模块。寄身运行态解释归前端 `ItemGrafts/`，游戏事实观察归后端
`ItemGrafts/`。

## 方向

前端向后端登记当前宿主：

- `RegisterHostRequest`：携带当前宿主 `HostKey`，要求后端只追踪这个宿主的删除和角色行囊转移事实。
- `UnregisterHostRequest`：清空后端登记。它不携带 key，含义是前端当前没有要后端追踪的宿主。

后端向前端报告事实：

- `TaiwuInventorySnapshotChangedRequest`：太吾角色行囊快照已被游戏写入。它不携带物品 id；用途是告诉前端当前行囊数据
  进入可读边界，由前端重新扫描并决定附着或创建宿主。
- `InventoryTransferRequest`：已登记宿主在角色行囊端点之间转移，携带 `HostKey`、`FromCharacterId`、`ToCharacterId`
  和 `Amount`。`NoCharacterInventoryId` 只表示该端点不属于任何角色行囊，不表示未知角色或查询失败。
- `HostRemovedRequest`：已登记宿主物品实例被游戏删除，携带 `HostKey`。前端收到后按当前宿主 key 复核，再清空寄身
  运行态并进入重新附着或创建宿主的路径。

## 约定

- 携带宿主语义的请求始终携带 `HostKey`。
- 角色行囊转移和物品实例删除是两个不同事实，不共用 DTO。
- `TaiwuInventorySnapshotChangedRequest` 只表示可重新读取太吾行囊快照，不代表任何指定宿主的转移、删除或存在性。
- 本协议不持久化相枢寄身状态；重新读档后的宿主选择由前端运行态按当前行囊重建。
- DTO 名位于 `Wanxiang.Xiangshu.Ipc.ItemGrafts` 命名空间内，不重复 `Ipc` 或 `ItemGraft` 前缀；命名保留事件和动作差异。
