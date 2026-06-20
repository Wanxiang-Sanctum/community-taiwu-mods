# Wanxiang.Taiwu.ItemGrafts

太吾绘卷前端行囊物品嫁接协议的共享项目。

“嫁接”指使用一个后端真实存在的非堆叠 `ItemKey` 作为宿主，在已适配的前端行囊入口里呈现另一套名称、描述、图标、品级和操作。
后端、存档、交易、丢弃、转移以及其它未被使用方拦截的游戏交互，仍然处理原来的真实物品。

本库提供两个异步动作：把已有宿主嫁接为 `Graft`，以及创建真实宿主后立即建立 `Graft`。两个动作都只返回前端嫁接状态；
使用方负责把它放入自己的内存集合，并在自己的 UI 适配层中应用这些状态。

## 边界

本项目是前端共享 DLL，目标框架为 `netstandard2.1`，引用 `Taiwu.ModKit.References.Frontend` 和
`Taiwu.ModKit.Dependencies.UniTask`，只面向前端插件。

`InventoryGrafts.CreateAsync(...)` 通过游戏原生 `CharacterDomainMethod.Call.CreateInventoryItem(...)` 创建真实宿主，再从前端行囊快照定位该宿主。
本库不写入真实物品字段，也不保存业务状态。

## 协议模型

`Graft` 是已嫁接宿主的前端状态。它绑定一个有效的非堆叠真实 `ItemKey`，并携带外观、操作和菜单策略。

`GraftDefinition` 是建立 `Graft` 的输入，由 `GraftAppearance`、`GraftMenuMode` 和 `GraftOperation` 列表组成。
`GraftAppearance` 描述名称、描述、图标和品级替换；字段为 `null` 时沿用宿主原值。`GraftOperation` 描述前端可展示的自定义操作，
执行时接收宿主 `ItemKey`。

`GraftMenuMode` 必须显式传入。`Append` 保留原生菜单并追加嫁接操作；`Replace` 用嫁接操作完整替换该物品的前端菜单。
没有嫁接操作时传入空 `operations` 列表；空列表与 `Replace` 组合时，表示该嫁接物没有前端菜单操作。

`HostTemplate` 描述创建宿主所需的 `itemType` 和 `templateId`。宿主模板必须有效且非堆叠；堆叠物会复用同一个模板 key，
不能稳定代表“这一件行囊物品”。

## 动作

`InventoryGrafts.AttachAsync(...)` 嫁接已有宿主。它接收宿主 `ItemKey`、`GraftDefinition` 和可选的 `AttachOptions`，
返回 `UniTask<Graft>`。

`InventoryGrafts.CreateAsync(...)` 创建已嫁接物。它接收角色 ID、`HostTemplate`、`GraftDefinition` 和可选的 `CreateOptions`。
执行时读取行囊快照、创建宿主、刷新对应物品子类的行囊列表、定位宿主，再返回 `UniTask<Graft>`。
默认定位规则从创建前后快照的差集中选择同模板 `RealKey`；未找到新宿主时动作失败。需要自行处理同模板宿主歧义时，
设置 `CreateOptions.SelectHost`；选择结果必须匹配请求创建的宿主模板。

`AttachOptions` 和 `CreateOptions` 只承载动作级附加行为，当前用于即时通知。

## 使用路径

嫁接已有物品时，使用方已经持有一个真实宿主 `ItemKey`：

```csharp
Dictionary<ItemKey, Graft> graftsByHost = [];

GraftDefinition definition = new(
    appearance: new GraftAppearance(
        name: "相枢札记",
        description: "这只是一件真实物品在相枢行囊入口里的临时面貌。",
        iconName: "ui_icon_book_001",
        grade: 3),
    menuMode: GraftMenuMode.Replace,
    operations:
    [
        new GraftOperation("查看", OpenNoteForItem),
    ]);

Graft graft = await InventoryGrafts.AttachAsync(
    hostKey: existingHostKey,
    definition: definition,
    options: new AttachOptions
    {
        NotificationMessage = "粗瓷促织罐新增了相枢札记入口。",
    });

graftsByHost[graft.HostKey] = graft;
```

创建已嫁接物时，使用方只描述宿主模板和嫁接外观：

```csharp
Dictionary<ItemKey, Graft> graftsByHost = [];

GraftDefinition definition = new(
    appearance: new GraftAppearance(
        name: "相枢札记",
        description: "这只是一件真实物品在相枢行囊入口里的临时面貌。",
        iconName: "ui_icon_book_001",
        grade: 3),
    menuMode: GraftMenuMode.Replace,
    operations:
    [
        new GraftOperation("查看", OpenNoteForItem),
    ]);

Graft graft = await InventoryGrafts.CreateAsync(
    characterId: taiwuCharId,
    hostTemplate: new HostTemplate(
        itemType: 4,
        templateId: 26),
    definition: definition,
    options: new CreateOptions
    {
        NotificationMessage = "相枢札记已加入行囊。",
    });

graftsByHost[graft.HostKey] = graft;
```

`CreateAsync` 内部用 UniTask 包装游戏的 `GetInventoryItems` 回调；调用方只需要 await 结果。动作返回后，使用方可以用
`graft.HostKey` 保存自己的业务锚点。

```csharp
static void OpenNoteForItem(ItemKey hostKey)
{
    // 使用方 mod 在这里打开自己的 UI，或请求后端读取自己的 mod archive data。
}
```

## 通知

两个动作默认不推送通知。需要通知时，在 `AttachOptions` 或 `CreateOptions` 上设置 `NotificationMessage`：

```csharp
new AttachOptions
{
    NotificationMessage = "粗瓷促织罐新增了相枢札记入口。",
};

new CreateOptions
{
    NotificationMessage = "相枢札记已加入行囊。",
    NotificationRecordType = 254,
};
```

动作会把 `NotificationMessage` 原文推送为即时通知。通知默认复用游戏现有的物品类即时通知模板作为图标和底色；
需要改用其它原生模板时，设置 `NotificationRecordType`。`NotificationRecordType` 只影响原生通知外观，不改变文本内容。

需要脱离嫁接动作单独推送文本通知时，调用 `GraftNotifications.Push(...)`。

即时通知只向当前前端的 `DisplayTriggerModel.RenderedNotificationList` 追加一条 `NotificationItem`，再触发 `UiEvents.OnNewInstantNotification`。
它没有后端副作用，不写存档，也不会成为游戏真实机制的一部分。

## 状态归属

本库不定义存档格式，也不提供 `ItemKey` 到 `Graft` 的全局表。

需要随存档保存时，使用方应把自己的业务数据写入自己的 mod archive data，并用宿主 `ItemKey` 作为锚点。前端恢复时先从后端
读取真实行囊，确认宿主物品仍存在，再重新建立 `Graft`。如果宿主物品不存在，使用方再决定是不显示、提示失效，还是执行
`InventoryGrafts.CreateAsync(...)` 重新创建一个宿主并建立嫁接。

使用方 mod 已经需要维护自己关心的行囊物品 ID，直接用自己的集合或字典即可。
跨 mod 同时接管同一真实物品时，本库不提供仲裁；相关 UI 适配层或前置依赖负责定义优先级。

## 开发

从仓库根目录构建：

```powershell
dotnet build shared/Wanxiang.Taiwu.ItemGrafts/Wanxiang.Taiwu.ItemGrafts.csproj
```

共享项目不作为独立插件入口写入 mod 包。引用它的前端插件项目负责决定将
`Wanxiang.Taiwu.ItemGrafts.dll` 合并、复制或随插件部署。
