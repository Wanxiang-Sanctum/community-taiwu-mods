# Wanxiang.Taiwu.ItemGrafts.Frontend

太吾绘卷前端行囊物品嫁接实现项目。

“嫁接”指使用一个游戏后端真实存在的非堆叠 `ItemKey` 作为宿主，在已适配的前端行囊入口里呈现另一套名称、描述、图标、
品级和操作。游戏后端、存档、交易、丢弃、转移以及其它未被使用方拦截的游戏交互，仍然处理原来的真实物品。

本项目提供两个前端动作：把已有宿主建立为 `GraftSession`，以及创建真实宿主后立即建立 `GraftSession`。动作成功返回时，
前端嫁接状态已经建立，后端也已经开始观察宿主事实；调用方保存返回的 session，并在自己的 UI 适配层中应用
`session.Graft`。

## 边界

本项目是 ItemGrafts 的前端动作和会话实现。它面向 `netstandard2.1` 前端插件，引用
`Taiwu.ModKit.References.Frontend`、`Taiwu.ModKit.Dependencies.UniTask`、`Wanxiang.Taiwu.AsyncInterop` 和
`Wanxiang.Taiwu.ModRpc`。

使用 `InventoryGrafts.AttachAsync(...)` / `CreateAsync(...)` 前，前端需在插件初始化时调用
`InventoryGrafts.Install(this)`。该安装绑定本 mod id，使前端 `GraftSession` 可以通过 `Wanxiang.Taiwu.ModRpc`
与同一 mod 的后端观察服务协作。后端观察服务由 `Wanxiang.Taiwu.ItemGrafts.Backend` 安装和释放；前端没有全局卸载入口，
每个会话资源由对应的 `GraftSession.DisposeAsync()` 释放。

`InventoryGrafts.CreateAsync(...)` 调用游戏的创建行囊物品能力创建真实宿主，再从前端行囊快照定位该宿主。本项目不写入
真实物品字段，不保存使用方状态，也不改变未适配交互里的原始物品表现。

## 前端模型

`GraftSession` 是一次前端嫁接会话。它持有 `Graft`，把宿主事件交给创建时传入的回调，并管理宿主订阅生命周期。

`Graft` 是已嫁接宿主的前端状态。它绑定一个有效的非堆叠真实 `ItemKey`，并携带稳定的 `GraftHostId`、外观、操作和菜单策略。
外部代码不直接构造 `Graft`；通过 `InventoryGrafts.AttachAsync(...)` / `CreateAsync(...)` 得到 `GraftSession`，
再从 `session.Graft` 读取已校验的状态。

`GraftHostId` 是宿主实例身份，由物品类型、模板 ID 和物品实例 ID 组成，不包含 `ModificationState`。`Graft.HostKey`
是当前可传给游戏 API 的完整 key；宿主精炼、淬毒等数据变化可能让完整 key 改变，session 会在收到宿主事件时更新
`Graft.HostKey`。长期索引、字典 key 和存档锚点应优先使用 `GraftHostId`。

`GraftDefinition` 是建立 `Graft` 的输入，由 `GraftAppearance`、`GraftMenuMode` 和 `GraftOperation` 列表组成。
`GraftAppearance` 描述名称、描述、图标和品级替换；字段为 `null` 时沿用宿主原值。`GraftOperation` 描述前端可展示的
自定义操作；启用操作执行时接收宿主 `ItemKey`，禁用操作只提供标签和原因。

`GraftMenuMode` 必须显式传入。`Append` 保留原生菜单并追加嫁接操作；`Replace` 用嫁接操作完整替换该物品的前端菜单。
没有嫁接操作时传入空 `operations` 列表；空列表与 `Replace` 组合时，表示该嫁接物没有前端菜单操作。

`GraftHostTemplate`、`GraftHostId`、`GraftAppearance` 和 `GraftHostEventArgs` 来自
`Wanxiang.Taiwu.ItemGrafts.Contracts`。本项目只负责在前端动作和 session 生命周期里使用这些契约；宿主身份、事件种类和
跨端边界以 Contracts README 为准。

## 动作

`InventoryGrafts.AttachAsync(...)` 嫁接已有宿主。它用于使用方已经从行囊数据中选定宿主的场景，接收宿主 `ItemKey`、
`GraftDefinition` 和可选的 `AttachOptions`，返回 `UniTask<GraftSession>`。

`InventoryGrafts.CreateAsync(...)` 创建已嫁接物。它接收角色 ID、`GraftHostTemplate`、`GraftDefinition` 和可选的
`CreateOptions`。执行时创建一个数量为 1 的真实宿主，再从行囊快照中定位新宿主并返回 `UniTask<GraftSession>`。
默认定位规则从创建前后快照的差集中选择同模板 `ItemDisplayData.RealKey`；快照里没有可匹配真实宿主时动作失败。
需要自行处理同模板宿主歧义时，
设置 `CreateOptions.SelectCreatedHost`；该选择器接收创建前、创建后的同子类行囊列表，返回结果必须匹配请求创建的宿主模板。

`AttachOptions` 承载动作级即时通知和宿主事件回调；`CreateOptions` 同时承载即时通知、宿主事件回调和创建宿主后的定位规则。
宿主事件回调通过 `OnHostEvent` 传入建会话动作，避免 session 返回后再登记回调而漏掉后端推送。

## 调用方式

示例以 `CraftTool.DefKey.Medicine0`（陶土药钵）作为非堆叠真实宿主。

前端初始化时绑定本 mod id：

```csharp
InventoryGrafts.Install(this);
```

定义嫁接内容：

```csharp
Dictionary<GraftHostId, GraftSession> sessionsByHost = [];

GraftDefinition definition = new(
    appearance: new GraftAppearance(
        name: "低语的陶土药钵",
        description: "药杵未动，钵底却传出细碎低语，自称相枢。",
        iconName: CraftTool.DefValue.Medicine0.Icon,
        grade: CraftTool.DefValue.Medicine0.Grade),
    menuMode: GraftMenuMode.Replace,
    operations:
    [
        new GraftOperation("查看", OpenNoteForItem),
    ]);
```

嫁接已有物品时，使用方已经持有一个真实宿主 `ItemKey`：

```csharp
GraftSession session = await InventoryGrafts.AttachAsync(
    hostKey: existingMedicineBowlKey,
    definition: definition,
    options: new AttachOptions
    {
        NotificationMessage = "相枢藏进了陶土药钵。",
        OnHostEvent = HandleHostEvent,
    });

sessionsByHost[session.Graft.HostId] = session;
```

创建已嫁接物时，使用方提供宿主模板；创建、定位和宿主订阅由该动作完成：

```csharp
GraftSession session = await InventoryGrafts.CreateAsync(
    characterId: taiwuCharId,
    hostTemplate: new GraftHostTemplate(
        itemType: ItemType.CraftTool,
        templateId: CraftTool.DefKey.Medicine0),
    definition: definition,
    options: new CreateOptions
    {
        NotificationMessage = "低语的陶土药钵落入了行囊。",
        OnHostEvent = HandleHostEvent,
    });

sessionsByHost[session.Graft.HostId] = session;
```

动作返回后，使用方可以用 `session.Graft.HostId` 保存自己的状态锚点；执行游戏调用或嫁接操作时，再使用当前
`session.Graft.HostKey`。

```csharp
static void OpenNoteForItem(ItemKey hostKey)
{
    // 使用方在这里处理自定义操作。
}

static void HandleHostEvent(GraftHostEventArgs hostEvent)
{
    switch (hostEvent)
    {
        case GraftHostLocationChangedEventArgs locationChanged:
            _ = locationChanged.FromCharacterId;
            _ = locationChanged.ToCharacterId;
            // 使用方在这里处理宿主行囊位置变化。
            break;
        case GraftHostDataChangedEventArgs:
            // 使用方在这里重新查询关心的宿主数据。
            break;
    }
}
```

## 会话生命周期

`GraftSession` 是嫁接能否继续应用的所有权边界。`IsActive` 为 `true` 时，调用方可以继续在自己的 UI 适配层应用
`session.Graft`；`IsActive` 为 `false` 后，调用方应停止应用该 session。

`EndReason` 在会话活跃时为 `null`，结束后说明原因：

- `Canceled`：调用方释放 session，取消本次嫁接会话；真实宿主物品仍由游戏或上层业务持有。
- `HostRemoved`：后端观察到真实宿主已被游戏流程删除，会话随宿主事实结束。

调用 `GraftSession.DisposeAsync()` 是调用方主动取消嫁接会话的唯一入口。它会取消本次宿主订阅并移除本地事件订阅，
但不会删除真实宿主物品。若宿主被游戏流程删除，后端会推送 `Removed` 事件，session 自动结束并把 `EndReason`
设为 `HostRemoved`。

后端还会推送 `LocationChanged` 和 `DataChanged`。`LocationChanged` 表示宿主进入、离开或转移到角色行囊；
`FromCharacterId` 和 `ToCharacterId` 是变化前后的角色行囊端点，非角色行囊端用 `null` 表示。
`DataChanged` 表示宿主真实物品数据已经变化，具体字段由使用方按自己的 UI 需要重新查询。前端可以通过
`AttachOptions.OnHostEvent` 或 `CreateOptions.OnHostEvent` 处理这些事件。

## 通知

两个动作默认不推送通知。需要通知时，在 `AttachOptions` 或 `CreateOptions` 上设置 `NotificationMessage`；
需要替换原生即时通知模板时，再设置具体的 `NotificationRecordType`：

```csharp
new CreateOptions
{
    NotificationMessage = "低语的陶土药钵落入了行囊。",
    NotificationRecordType = customNativeRecordType,
};
```

动作成功建立 `GraftSession` 后，才会把 `NotificationMessage` 原文推送为即时通知。`NotificationRecordType`
只影响原生通知外观，不改变文本内容。

即时通知没有后端副作用，不写存档，也不会成为游戏真实机制的一部分。

## 状态归属

使用方拥有持久化状态。本项目不定义存档格式，也不提供 `GraftHostId` 到 `GraftSession` 的全局表。

需要随存档恢复时，使用方自行持久化状态，并用宿主 `GraftHostId` 作为锚点。前端恢复时先读取真实行囊，确认宿主物品仍存在，
再重新建立 `GraftSession`。如果宿主物品不存在，恢复流程以未建立 session 结束；后续策略由使用方负责。

使用方应直接用自己的集合或字典维护关心的 `GraftHostId`。同一个宿主可以被多个前端 session 订阅；后端按宿主身份统计 session，
最后一个 session 结束后才会停止观察。同一 mod 内多处 UI 或业务同时接管同一宿主时，本项目不提供仲裁；需要仲裁时，
由更高层 UI 适配定义优先级。

## 开发

从仓库根目录构建：

```powershell
dotnet build shared/Wanxiang.Taiwu.ItemGrafts.Frontend/Wanxiang.Taiwu.ItemGrafts.Frontend.csproj
```

共享项目不作为独立插件入口写入 mod 包。引用它的前端插件项目负责决定将
`Wanxiang.Taiwu.ItemGrafts.Frontend.dll` 合并、复制或随插件部署。
