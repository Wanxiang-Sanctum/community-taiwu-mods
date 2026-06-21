# Wanxiang.Taiwu.ItemGrafts.Contracts

行囊物品嫁接机制的跨端契约项目。

“嫁接”使用一个游戏后端真实存在的非堆叠 `ItemKey` 作为宿主，在前端行囊入口呈现替换后的名称、描述、图标、品级和操作。
前端负责建立和结束嫁接会话，后端负责观察宿主存在性、角色行囊位置和宿主数据变化。本项目是这些稳定模型的来源；
创建宿主、菜单操作、通知、Harmony 观察和会话统计分别由端侧项目负责。

## 公开模型

- `GraftHostTemplate`：请求创建真实宿主时使用的物品类型和模板 ID；是否可作为非堆叠宿主由端侧动作校验。
- `GraftHostId`：宿主实例身份，由物品类型、模板 ID 和物品实例 ID 组成，不包含 `ModificationState`。
- `GraftAppearance`：嫁接物在前端展示时可替换的名称、描述、图标和品级。
- `GraftHostEventArgs`：后端观察到宿主删除、角色行囊位置变化或宿主数据变化后发回前端的宿主事件基类。
- `GraftHostEventKind`：`GraftHostEventArgs` 的事件种类枚举。

宿主事件只做基础事实分类：`Removed`、`LocationChanged`、`DataChanged`。`DataChanged` 是重新查询信号，
不承诺解释具体字段或游戏行为原因；使用方收到后按自己的 UI 和业务需要读取真实物品数据。

事件对外按可辨识联合风格表达：`GraftHostRemovedEventArgs`、`GraftHostLocationChangedEventArgs`、
`GraftHostDataChangedEventArgs` 分别承载各自有意义的数据。行囊宿主限定为非堆叠物，位置变化不携带数量。
`GraftHostLocationChangedEventArgs` 的 `FromCharacterId` 和 `ToCharacterId` 是角色行囊端点；`null` 表示该端不是角色行囊。

## 模块边界

`Wanxiang.Taiwu.ItemGrafts.Frontend` 负责前端入口：`InventoryGrafts.Install(plugin)`、`AttachAsync(...)`、
`CreateAsync(...)`、`GraftSession`、菜单操作和即时通知。

`Wanxiang.Taiwu.ItemGrafts.Backend` 负责后端入口：`BackendInventoryGrafts.Install(plugin)`、宿主会话统计、
Harmony 观察和宿主事件转发。

本项目不提供可独立调用的嫁接动作，也不保存使用方状态。需要随存档恢复时，使用方保存自己的业务状态，并用
`GraftHostId` 作为宿主锚点；恢复流程由端侧项目重新建立会话。

## 内部协议

`Internal/` 下的宿主校验和 RPC 协议编解码是端侧实现共享的内部源文件；它们通过项目链接编译进前端和后端，
不通过 `InternalsVisibleTo` 扩大程序集边界。

调用方不直接拼 ItemGrafts RPC 内容或后端补丁标识。前端通过 `InventoryGrafts.Install(plugin)` 绑定本 mod，
再通过 `AttachAsync(...)` / `CreateAsync(...)` 建立 `GraftSession`；后端通过 `BackendInventoryGrafts.Install(plugin)`
安装观察和转发。
