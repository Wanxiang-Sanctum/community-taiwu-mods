# Wanxiang.Taiwu.ItemGrafts

太吾绘卷前端行囊物品嫁接协议的共享项目。

“嫁接”指使用一个后端真实存在的非堆叠 `ItemKey` 作为宿主，在已适配的前端行囊入口里呈现另一套名称、描述、图标、品级和操作。
后端、存档、交易、丢弃、转移以及其它未被使用方拦截的游戏交互，仍然处理原来的真实物品。

本库提供两个异步动作：把已有宿主嫁接为 `Graft`，以及创建真实宿主后立即建立 `Graft`。两个动作都只返回前端嫁接状态；
使用方负责保存返回的 `Graft`，并在自己的 UI 适配层中应用这些状态。

## 边界

本项目是前端共享 DLL，目标框架为 `netstandard2.1`，引用 `Taiwu.ModKit.References.Frontend` 和
`Taiwu.ModKit.Dependencies.UniTask`，只面向前端插件。

`InventoryGrafts.CreateAsync(...)` 通过游戏原生 `CharacterDomainMethod.Call.CreateInventoryItem(...)` 创建真实宿主，再从前端行囊快照定位该宿主。
本库不写入真实物品字段，不保存使用方状态，也不改变未适配交互里的原始物品表现。

## 协议模型

`Graft` 是已嫁接宿主的前端状态。它绑定一个有效的非堆叠真实 `ItemKey`，并携带外观、操作和菜单策略。

`GraftDefinition` 是建立 `Graft` 的输入，由 `GraftAppearance`、`GraftMenuMode` 和 `GraftOperation` 列表组成。
`GraftAppearance` 描述名称、描述、图标和品级替换；字段为 `null` 时沿用宿主原值。`GraftOperation` 描述前端可展示的自定义操作；
启用操作执行时接收宿主 `ItemKey`，禁用操作只提供标签和原因。

`GraftMenuMode` 必须显式传入。`Append` 保留原生菜单并追加嫁接操作；`Replace` 用嫁接操作完整替换该物品的前端菜单。
没有嫁接操作时传入空 `operations` 列表；空列表与 `Replace` 组合时，表示该嫁接物没有前端菜单操作。

`HostTemplate` 描述创建宿主所需的 `itemType` 和 `templateId`。宿主模板必须有效且非堆叠；堆叠物会复用同一个模板 key，
不能稳定代表“这一件行囊物品”。

## 动作

`InventoryGrafts.AttachAsync(...)` 嫁接已有宿主。它用于使用方已经从行囊数据中选定宿主的场景，接收宿主 `ItemKey`、
`GraftDefinition` 和可选的 `AttachOptions`，返回 `UniTask<Graft>`。

`InventoryGrafts.CreateAsync(...)` 创建已嫁接物。它接收角色 ID、`HostTemplate`、`GraftDefinition` 和可选的 `CreateOptions`。
执行时创建一个数量为 1 的真实宿主，再从行囊快照中定位新宿主并返回 `UniTask<Graft>`。
默认定位规则从创建前后快照的差集中选择同模板 `RealKey`；未找到新宿主时动作失败。需要自行处理同模板宿主歧义时，
设置 `CreateOptions.SelectHost`；该选择器接收创建前、创建后的同子类行囊列表，返回结果必须匹配请求创建的宿主模板。

`AttachOptions` 承载动作级即时通知；`CreateOptions` 同时承载即时通知和创建宿主后的定位规则。

## 调用方式

示例以 `CraftTool.DefKey.Medicine0`（陶土药钵）作为非堆叠真实宿主。

定义嫁接内容：

```csharp
Dictionary<ItemKey, Graft> graftsByHost = [];

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
Graft graft = await InventoryGrafts.AttachAsync(
    hostKey: existingMedicineBowlKey,
    definition: definition,
    options: new AttachOptions
    {
        NotificationMessage = "相枢藏进了陶土药钵。",
    });

graftsByHost[graft.HostKey] = graft;
```

创建已嫁接物时，使用方提供宿主模板；创建和定位新宿主由该动作完成：

```csharp
Graft graft = await InventoryGrafts.CreateAsync(
    characterId: taiwuCharId,
    hostTemplate: new HostTemplate(
        itemType: ItemType.CraftTool,
        templateId: CraftTool.DefKey.Medicine0),
    definition: definition,
    options: new CreateOptions
    {
        NotificationMessage = "低语的陶土药钵落入了行囊。",
    });

graftsByHost[graft.HostKey] = graft;
```

动作返回后，使用方可以用 `graft.HostKey` 保存自己的状态锚点。

```csharp
static void OpenNoteForItem(ItemKey hostKey)
{
    // 使用方在这里处理自定义操作。
}
```

## 通知

两个动作默认不推送通知。需要通知时，在 `AttachOptions` 或 `CreateOptions` 上设置 `NotificationMessage`；
需要替换原生即时通知模板时，再设置 `NotificationRecordType`：

```csharp
new CreateOptions
{
    NotificationMessage = "低语的陶土药钵落入了行囊。",
    NotificationRecordType = GraftNotifications.DefaultNativeRecordType,
};
```

动作会把 `NotificationMessage` 原文推送为即时通知。`NotificationRecordType` 只影响原生通知外观，不改变文本内容。

需要脱离嫁接动作单独推送文本通知时，调用 `GraftNotifications.Push(...)`。

即时通知只向当前前端的 `DisplayTriggerModel.RenderedNotificationList` 追加一条 `NotificationItem`，再触发 `UiEvents.OnNewInstantNotification`。
它没有后端副作用，不写存档，也不会成为游戏真实机制的一部分。

## 状态归属

本库不定义存档格式，也不提供 `ItemKey` 到 `Graft` 的全局表。

需要随存档恢复时，使用方自行持久化状态，并用宿主 `ItemKey` 作为锚点。前端恢复时先从后端读取真实行囊，
确认宿主物品仍存在，再重新建立 `Graft`。如果宿主物品不存在，使用方再决定是不显示、提示失效，还是执行
`InventoryGrafts.CreateAsync(...)` 重新创建一个宿主并建立嫁接。

使用方应直接用自己的集合或字典维护关心的宿主 `ItemKey`。
跨 mod 同时接管同一宿主时，本库不提供仲裁；需要仲裁时，由更高层 UI 适配或前置依赖定义优先级。

## 开发

从仓库根目录构建：

```powershell
dotnet build shared/Wanxiang.Taiwu.ItemGrafts/Wanxiang.Taiwu.ItemGrafts.csproj
```

共享项目不作为独立插件入口写入 mod 包。引用它的前端插件项目负责决定将
`Wanxiang.Taiwu.ItemGrafts.dll` 合并、复制或随插件部署。
