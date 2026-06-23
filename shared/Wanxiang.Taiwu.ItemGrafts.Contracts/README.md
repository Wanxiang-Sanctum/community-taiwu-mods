# Wanxiang.Taiwu.ItemGrafts.Contracts

行囊物品嫁接机制的跨端契约项目。

“嫁接”使用一个游戏后端真实存在的非堆叠 `ItemKey` 作为宿主。Contracts 表达宿主身份、宿主模板、可选外观覆盖值和
宿主事件；它不规定某个前端入口如何渲染外观或菜单。前端负责建立和结束嫁接会话，并决定如何把这些契约应用到可视化
和操作入口；后端负责观察宿主存在性、角色行囊位置和宿主数据变化。Contracts 是稳定模型和跨端内部协议的所有者；
创建宿主、外观应用、菜单、通知、观察和会话统计分别由端侧项目负责。

## 公开模型

- `GraftHostTemplate`：请求创建真实宿主时使用的物品类型和模板 ID；构造时校验模板存在且不是堆叠物。
- `GraftHostId`：宿主实例身份，由物品类型、模板 ID 和物品实例 ID 组成，不包含 `ModificationState`。
- `GraftAppearance`：嫁接物可提供的名称、描述、详情描述、图标和视觉品级覆盖值；具体渲染由前端实现决定。
- `GraftHostEventArgs`：后端观察到宿主删除、角色行囊位置变化或宿主数据变化后发回前端的宿主事件基类。
- `GraftHostEventKind`：`GraftHostEventArgs` 的事件种类枚举。

`GraftAppearance` 属于外观契约，不是物品事实模型。字符串覆盖为空白时按未提供处理；`VisualGrade`
是可选透传值，本契约不维护品级枚举、常量、范围或显示规则。宿主品级、类型、价值、重量、耐久和其它游戏事实
仍由真实宿主决定。

宿主事件表达基础事实分类：`Removed`、`LocationChanged`、`DataChanged`。`DataChanged` 是重新查询信号；
字段级解释和后续展示策略归使用方，使用方收到后按自己的 UI 和业务需要读取真实物品数据。

事件对外按可辨识联合风格表达：`GraftHostRemovedEventArgs`、`GraftHostLocationChangedEventArgs`、
`GraftHostDataChangedEventArgs` 分别承载各自有意义的数据。行囊宿主限定为非堆叠物，位置变化不携带数量。
`GraftHostLocationChangedEventArgs` 的 `FromCharacterId` 和 `ToCharacterId` 是角色行囊端点；`null` 表示该端不是角色行囊。

## 模块边界

`Wanxiang.Taiwu.ItemGrafts.Frontend` 负责前端入口：`InventoryGrafts.Install(plugin)`、`AttachAsync(...)`、
`CreateAsync(...)`、`GraftSession`、共享可视化、菜单操作和即时通知。

`Wanxiang.Taiwu.ItemGrafts.Backend` 负责后端入口：`BackendInventoryGrafts.Install(plugin)`、宿主会话统计、
Harmony 观察和宿主事件转发。

嫁接动作归端侧项目，业务状态归使用方。需要随存档恢复时，使用方保存自己的业务状态，并用 `GraftHostId`
作为宿主锚点；恢复流程由端侧项目重新建立会话。

## 内部协议

`Internal/` 下的宿主校验和 RPC 协议编解码归本程序集持有，作为前端和后端两个指定实现程序集之间的内部机制。
这两个实现程序集通过 `InternalsVisibleTo` 使用内部协议；普通调用方只依赖 `GraftHostId`、`GraftHostTemplate`、
宿主事件等公开契约。

ItemGrafts RPC 使用显式 payload 字段；缺省字段表示该事件或请求没有对应值。
RPC 内容和后端观察标识由前后端实现程序集管理。前端通过 `InventoryGrafts.Install(plugin)` 绑定本 Mod，
再通过 `AttachAsync(...)` / `CreateAsync(...)` 建立 `GraftSession`；后端通过
`BackendInventoryGrafts.Install(plugin)` 安装观察和转发。
