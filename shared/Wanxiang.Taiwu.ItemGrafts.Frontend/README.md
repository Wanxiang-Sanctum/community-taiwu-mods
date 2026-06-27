# Wanxiang.Taiwu.ItemGrafts.Frontend

太吾绘卷前端物品嫁接实现项目。

“嫁接”使用一个游戏后端真实存在的非堆叠 `ItemKey` 作为宿主。本项目在当前支持的物品显示入口应用
`GraftAppearance` 外观覆盖，在太吾行囊入口提供 `Replace` 菜单适配，并在携带真实 `ItemKey` 的物品消息入口里替换
实例名称。游戏后端、存档、交易、丢弃、转移以及其它未被使用方接管的游戏交互，仍然处理原来的真实物品。

本项目提供两个前端动作：把已有宿主建立为 `GraftSession`，以及创建真实宿主后立即建立 `GraftSession`。动作成功返回时，
前端会话、共享可视化状态和后端宿主订阅都已经建立；使用方保存返回的会话，用于业务状态、生命周期和持久化锚点。

## 边界

本项目是 ItemGrafts 的前端动作和会话实现。它面向 `netstandard2.1` 前端插件，引用
`Taiwu.ModKit.References.Frontend`、`Taiwu.ModKit.Dependencies.UniTask`、`Wanxiang.Taiwu.AsyncInterop` 和
`Wanxiang.Taiwu.ModRpc`。动作级即时通知通过 `Wanxiang.Taiwu.InstantNotifications` 发布，游戏通知展示适配由该共享项目拥有。

使用 `ItemGraftActions.AttachAsync(...)` / `CreateAsync(...)` 前，前端需在插件初始化时调用
`ItemGraftActions.Install(this)`。该安装绑定本 Mod ID，使前端 `GraftSession` 可以通过
`Wanxiang.Taiwu.ModRpc` 与同一 Mod 的后端创建与观察服务协作；它也安装共享前端可视化层。后端服务由
`Wanxiang.Taiwu.ItemGrafts.Backend` 安装和释放。

`ItemGraftActions.Uninstall()` 卸载共享前端可视化层并清空内部显示状态；它不释放已经返回给使用方的会话。
每个会话资源仍由对应的 `GraftSession.DisposeAsync()` 释放。

`ItemGraftActions.CreateAsync(...)` 把游戏 owner 和宿主模板发给同一 Mod 的后端，由后端创建真实宿主，并通过游戏集合 API 写入目标 owner 后返回
新宿主 `ItemKey`。本项目不为使用方选择业务 owner；真实物品字段、使用方状态和未适配交互表现分别归游戏流程或使用方。本项目负责
发起创建、建立前端嫁接会话，并在支持的前端入口提供共享可视化。

## 前端模型

`GraftSession` 是一次前端嫁接会话。它持有 `Graft`，把宿主事件交给创建时传入的回调，并管理宿主订阅生命周期。

`Graft` 是已嫁接宿主的前端状态。它绑定一个有效的非堆叠真实 `ItemKey`，并携带稳定的 `GraftHostId`、外观、操作和菜单策略。
会话通过 `ItemGraftActions.AttachAsync(...)` / `CreateAsync(...)` 产生；使用方从返回的 `session.Graft` 读取已校验状态。

`GraftHostId` 是宿主实例身份，由物品类型、模板 ID 和物品实例 ID 组成，不包含 `ModificationState`。`Graft.HostKey`
是当前可传给游戏 API 的完整 key；宿主精炼、淬毒等数据变化可能让完整 key 改变，会话会在收到宿主事件时更新
`Graft.HostKey`。长期索引、字典 key 和存档锚点应优先使用 `GraftHostId`。

`GraftDefinition` 是建立 `Graft` 的输入，由 `GraftAppearance`、`GraftMenuMode` 和 `GraftOperation` 列表组成。
`GraftAppearance` 来自 Contracts，描述名称、描述、详情描述、图标和视觉品级覆盖；字符串字段为 `null` 或空白时沿用宿主原值，
`VisualGrade` 为 `null` 时不提供视觉品级覆盖。`GraftOperation` 描述前端可展示的自定义操作；启用操作执行时接收宿主
`ItemKey`，禁用操作只提供标签和原因文案，不表达宿主固有能力或真实数值。

`GraftHostOwnerKey`、`GraftHostTemplate`、`GraftHostId`、`GraftAppearance` 和 `GraftHostEventArgs` 来自
`Wanxiang.Taiwu.ItemGrafts.Contracts`。本项目只在前端动作和会话生命周期里使用这些契约；owner 传输、宿主身份、
事件种类和跨端协议归 Contracts 项目说明。

## 可视化与菜单

共享前端可视化层维护一份内部显示状态，只用于判断哪些宿主物品应应用嫁接外观。物品 owner 判定、业务状态和存档恢复
归使用方自己的状态管理；使用方维护的 `GraftHostId` 到 `GraftSession` 索引不作为该可视化层的依赖。

该可视化层在本项目支持的物品显示入口应用 `GraftAppearance`。行囊物品组件、常规物品提示和制造工具提示只在对应控件
存在时应用名称、描述、详情描述、图标和视觉品级覆盖；未提供的字段沿用真实宿主。这个支持范围只描述前端显示入口，
不限制 `CreateAsync(...)` 可接收的游戏 owner 类型。
`VisualGrade` 的职责限定在外观：当前支持入口可用它渲染名称颜色、品级底图和品级图标。品阶文案由宿主品级生成；类型、
价值、重量、耐久和其它游戏事实始终沿用真实宿主。`VisualGrade` 作为外观值透传给支持入口，具体取值语义以游戏和使用方选择为准。
携带真实 `ItemKey` 的游戏消息文本只替换实例名称，图标、引号、颜色和其它行内格式沿用游戏原生渲染结果。只携带物品类型和
模板 ID、没有实例 `ItemKey` 的文本无法判断具体宿主，保持原模板表现。

`GraftMenuMode` 必须显式传入。它是嫁接定义交给菜单适配的策略值。共享可视化层在太吾行囊入口实现 `Replace`：
用嫁接操作完整替换该物品的原生菜单。`Append` 表示保留原生菜单项并追加嫁接操作；它由使用方自有菜单代码解释，
不代表本项目提供通用菜单追加入口。没有嫁接操作时传入空 `operations` 列表；空列表与 `Replace` 组合时，表示该嫁接物没有
前端菜单操作。

## 动作

`ItemGraftActions.AttachAsync(...)` 嫁接已有宿主。它用于使用方已经持有真实宿主 `ItemKey` 的场景，接收宿主 `ItemKey`、
`GraftDefinition` 和可选的 `AttachmentOptions`，返回 `UniTask<GraftSession>`。

`ItemGraftActions.CreateAsync(...)` 创建宿主并建立嫁接会话。它接收 `GraftHostOwnerKey`、`GraftHostTemplate`、`GraftDefinition`
和可选的 `CreationOptions`。执行时请求后端创建一个数量为 1 的真实宿主，把它写入目标 owner 对应的游戏集合，使用后端返回的
`ItemKey` 建立嫁接会话并返回 `UniTask<GraftSession>`。目标 owner 没有对应游戏集合写入路径时，创建请求失败。

`AttachmentOptions` 和 `CreationOptions` 承载动作成功通知和宿主事件回调。
宿主事件回调通过 `OnHostEvent` 传入建会话动作，避免动作返回后再登记回调而漏掉后端推送。

## 调用方式

示例以 `CraftTool.DefKey.Medicine0`（陶土药钵）作为非堆叠真实宿主。

前端初始化时绑定本 Mod ID，并启用共享可视化层：

```csharp
ItemGraftActions.Install(this);
```

定义嫁接内容。示例中，`description` 描写物件异状，`detailDescription` 交代可用能力和离身边界；`visualGrade: 8`
只覆盖支持入口中的名称颜色、品级底图和品级图标，品阶文案仍显示宿主品阶：

```csharp
Dictionary<GraftHostId, GraftSession> sessionsByHost = [];

GraftDefinition definition = new(
    appearance: new GraftAppearance(
        name: "低语的陶土药钵",
        description: "陶钵泥胎微冷，药杵轻触便泛起低语，自称相枢，似有一缕万相回声寄在其中。",
        detailDescription:
            "轻叩钵沿，可问人物、局势与去路，也可托其查验、推演、尝试改易当前因果；" +
            "药钵离身则声息沉寂，复归身侧后方可续言。",
        iconName: CraftTool.DefValue.Medicine0.Icon,
        visualGrade: 8),
    menuMode: GraftMenuMode.Replace,
    operations:
    [
        new GraftOperation("对话", OpenXiangshuConversation),
    ]);
```

嫁接已有物品时，使用方已经持有一个真实宿主 `ItemKey`：

```csharp
GraftSession session = await ItemGraftActions.AttachAsync(
    hostKey: existingMedicineBowlKey,
    definition: definition,
    options: new AttachmentOptions
    {
        SuccessNotification = new GraftSuccessNotification(
            InstantNotification.DefKey.WalkThroughAbyss,
            "相枢藏进了陶土药钵。"),
        OnHostEvent = HandleHostEvent,
    });

sessionsByHost[session.Graft.HostId] = session;
```

创建宿主并立即建立嫁接会话时，使用方提供目标 owner 和宿主模板；创建、写入目标 owner 和宿主订阅由该动作完成：

```csharp
int taiwuCharId = SingletonObject.getInstance<BasicGameData>().TaiwuCharId;

GraftSession session = await ItemGraftActions.CreateAsync(
    targetOwner: GraftHostOwnerKey.CharacterInventory(taiwuCharId),
    hostTemplate: new GraftHostTemplate(
        itemType: ItemType.CraftTool,
        templateId: CraftTool.DefKey.Medicine0),
    definition: definition,
    options: new CreationOptions
    {
        SuccessNotification = new GraftSuccessNotification(
            InstantNotification.DefKey.WalkThroughAbyss,
            "低语的陶土药钵落入囊中。"),
        OnHostEvent = HandleHostEvent,
    });

sessionsByHost[session.Graft.HostId] = session;
```

动作返回后，使用方可以用 `session.Graft.HostId` 保存自己的状态锚点；执行游戏调用或嫁接操作时，再使用当前
`session.Graft.HostKey`。

```csharp
static void OpenXiangshuConversation(ItemKey hostKey)
{
    // 使用方在这里打开嫁接物自己的前端入口。
}

static void HandleHostEvent(GraftHostEventArgs hostEvent)
{
    switch (hostEvent)
    {
        case GraftHostOwnerChangedEventArgs ownerChanged:
            _ = ownerChanged.FromOwner;
            _ = ownerChanged.ToOwner;
            // 使用方在这里处理宿主 owner 变化。
            break;
        case GraftHostDataChangedEventArgs:
            // 使用方在这里重新查询关心的宿主数据。
            break;
    }
}
```

## 会话生命周期

`GraftSession` 是嫁接能否继续应用的所有权边界。`IsActive` 为 `true` 时，共享可视化层会继续应用该会话的
`Graft`；`IsActive` 为 `false` 后，该会话会从内部显示状态中移除，使用方也应从自己的业务索引移除该会话并停止应用
自有表现。

`EndReason` 在会话活跃时为 `null`，结束后说明原因：

- `Canceled`：调用方释放会话，取消本次嫁接会话；真实宿主物品仍由游戏或上层业务持有。
- `HostRemoved`：后端观察到真实宿主已被游戏流程删除，会话随宿主事实结束。

调用 `GraftSession.DisposeAsync()` 是调用方主动取消嫁接会话的唯一入口。它会取消本次宿主订阅并移除本地事件订阅，
但不会删除真实宿主物品。若宿主被游戏流程删除，后端会推送 `Removed` 事件，会话自动结束并把 `EndReason`
设为 `HostRemoved`。

后端还会推送 `OwnerChanged` 和 `DataChanged`。`OwnerChanged` 表示宿主的游戏 owner 已变化；
`FromOwner` 和 `ToOwner` 是变化前后的 owner，端点无 owner 时用 `null` 表示。
`DataChanged` 表示宿主真实物品数据已经变化，具体字段由使用方按自己的 UI 需要重新查询。前端可以通过
`AttachmentOptions.OnHostEvent` 或 `CreationOptions.OnHostEvent` 处理这些事件。

## 通知

两个动作默认不推送通知。需要在动作成功后提示玩家时，在 `AttachmentOptions` 或 `CreationOptions` 上设置
`SuccessNotification`。

`GraftSuccessNotification` 是一项完整的成功通知配置：`templateId` 选择游戏内置 `InstantNotification.DefKey`
模板，`message` 提供玩家可见文本。动作成功建立 `GraftSession` 后，本项目把这项配置交给
`Wanxiang.Taiwu.InstantNotifications` 推送为前端即时通知：

```csharp
new CreationOptions
{
    SuccessNotification = new GraftSuccessNotification(
        InstantNotification.DefKey.WalkThroughAbyss,
        "低语的陶土药钵落入囊中。"),
};
```

`SuccessNotification` 为 `null` 时不推送。通知文本必须非空白；原生模板只影响通知外观、通知类型和重要程度，
不改变玩家可见文本。即时通知属于前端展示结果；后端事实、存档状态和游戏真实机制仍由各自边界维护。

## 状态归属

使用方拥有持久化状态、存档格式和 `GraftHostId` 到 `GraftSession` 的运行期索引。

需要随存档恢复时，使用方自行持久化状态，并用宿主 `GraftHostId` 作为锚点。前端恢复时按自己的状态和游戏数据确认宿主物品仍存在，
再重新建立 `GraftSession`。如果宿主物品不存在，恢复流程以未建立会话结束；后续策略由使用方负责。

使用方应直接用自己的集合或字典维护关心的 `GraftHostId`。同一个宿主可以被多个前端会话订阅；后端按宿主身份统计会话，
最后一个会话结束后才会停止观察。同一 Mod 内多处 UI 或业务同时接管同一宿主时，仲裁归更高层 UI 适配定义。

## 开发

从仓库根目录构建：

```powershell
dotnet build shared/Wanxiang.Taiwu.ItemGrafts.Frontend/Wanxiang.Taiwu.ItemGrafts.Frontend.csproj
```
