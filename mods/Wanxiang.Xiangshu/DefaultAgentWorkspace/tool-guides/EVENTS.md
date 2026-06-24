# 事件运行时检索指引

当玩家询问当前正在发生的事件、待处理事件、事件节点、事件选项、选项为何出现或不可选、选项会跳向何处，
或想按事件文字、事件组、GUID 查找事件时，读取本文件。这里的“事件”指游戏 `TaiwuEvent` 运行时事件；角色的
最近经历、生平事件、经历分类和按经历找角色仍归 `LIFE_RECORDS.md`。事件属于后端运行态；优先检索显示数据
和事件运行时对象，再把结果整理为玩家可理解的答复。

本文件是事件的领域专门指引，负责说明检索模型、基础接口、可组合原语、返回数据和解释边界。脚本入口契约、
目标侧和线程规则仍归 `RUNTIME_SCRIPTING.md`；稳定机制、配置、本地化、模板显示辅助和百晓册资料仍归
`GAME_KNOWLEDGE.md`。

本文件随相枢 Mod 发布，面向订阅玩家的默认运行时；只依赖当前游戏进程中已加载的游戏程序集、配置表和相枢
脚本工具。运行时类型、成员和工具能力以实际工具结果为准。

## 查询模型

先判断玩家请求属于哪种检索方向：

- 读当前事件：玩家问眼前事件、当前事件正文、当前有哪些选项、某个当前选项为什么不可点。
- 读待处理事件：玩家问还有哪些事件待处理、过月事件队列、事件摘要或某个待处理事件是谁触发的。
- 找已加载事件：玩家给出事件 GUID、事件组、事件名片段、正文片段或选项文字，要求定位事件节点。
- 查选项逻辑：玩家问某个选项为何显示/隐藏/可用/不可用，或选择后会跳转、关闭、消耗、触发什么直接效果。

四种方向共用同一套解释规则：优先读取游戏已经生成的显示数据；显示数据不足时，再读事件配置和脚本结构；只有当前事件
带有 `ArgBox` 时才求值可见性/可用性。解释类查询停在只读检索；选项选择、事件跳转和脚本执行都按写操作处理。

## 读当前事件

默认处理顺序：

1. 在 `backend` 目标侧、`mainThread` 入口读取 `DomainManager.TaiwuEvent.GetDisplayingEventData()`。
2. 如果没有显示数据，再检查 `DomainManager.TaiwuEvent.ShowingEvent` 是否为空或 `IsEmpty`。
3. 返回 `EventGuid`、已替换的 `EventContent`、可见选项文本、`OptionState`、消耗和条件提示。
4. 玩家追问内部原因时，再读取 `ShowingEvent.EventConfig`、对应 `EventOption`、脚本和条件结构。

当前事件问题的主路径是显示数据和 `ShowingEvent`。需要核对玩家看到的 UI 文本或按钮位置时，回到 `PLAYER_VIEW.md`
取得玩家视图。

## 读待处理事件

待处理事件不是当前窗口内容。默认处理顺序：

1. 调用 `DomainManager.TaiwuEvent.GetTriggeredEventSummaryDisplayData()` 获取待处理事件摘要。
2. 摘要只稳定提供 `EventGuid` 和关联 `CharacterId`；需要事件组、正文或选项时，再对每个 `EventGuid` 调用
   `GetEvent(guid)`。
3. 需要角色名时，用 `CharacterId` 查角色显示数据，再返回玩家可读名称或称谓。
4. 结果很多时按队列顺序限量返回；玩家要求完整列表时再继续展开。

处理队列属于写操作；普通检索只读摘要。玩家明确要求处理时，按写操作规则先确认目标事件和影响范围。

## 找已加载事件

这类查询的主语不是当前窗口，而是“已加载事件集合 + 搜索谓词”。主路径是先用最窄身份定位，再有限扫描。

默认处理顺序：

1. 已知 GUID 时直接 `DomainManager.TaiwuEvent.GetEvent(guid)`。
2. 已知事件组、事件名片段、正文片段或选项文字时，遍历 `GetAllEventConfigs()` 做字符串匹配并限制返回数量。
3. 已知触发类型、事件类型或是否头事件时，用 `EventConfig.TriggerType`、`EventType`、`IsHeadEvent` 过滤。
4. 返回事件 GUID、事件组、事件类型、触发类型、选项数量和匹配到的正文/选项摘要。

当前运行时没有面向任意文本或条件的稳定事件索引；广域搜索由 `GetAllEventConfigs()` 扫描承担。普通查询先限量返回
候选，并说明还可以按事件组、正文、选项或触发类型继续收窄。

## 查选项逻辑

默认处理顺序：

1. 先确定目标事件：优先当前 `ShowingEvent`，其次玩家给出的 GUID，最后从搜索候选中选最相关节点。
2. 确定目标选项：优先用当前显示选项顺序或显示文本；调试语境才直接使用 `OptionKey`、`OptionGuid`。
3. 当前事件选项先读 `TaiwuEventDisplayData.EventOptionInfos`，其中 `OptionState`、`OptionConsumeInfos`、
   `OptionAvailableConditionInfos` 最接近玩家看到的结果。
4. 显示数据不足时，读取 `EventOption.IsVisible`、`IsAvailable`、`VisibleConditions`、`AvailableConditions`、
   `OptionAvailableConditions`、`Script` 和 `RedirectOption`。
5. 解释跳转时只读 `RedirectOption` 和 `Script` 结构；真正选择、跳转或执行脚本属于写操作。

非当前事件通常没有有效 `ArgBox`；这时只能解释静态结构，例如“有可见条件脚本”“有选中脚本”“重定向到某事件选项”，
这类结果应表述为结构摘要，不表述为当前可用性判断。

## 基础接口

后端核心域是 `GameData.Domains.TaiwuEvent`：

- `DomainManager.TaiwuEvent.GetDisplayingEventData()`：当前前端显示用事件数据。
- `DomainManager.TaiwuEvent.ShowingEvent`：当前后端正在处理的事件。
- `DomainManager.TaiwuEvent.GetTriggeredEventSummaryDisplayData()`：待处理事件摘要。
- `DomainManager.TaiwuEvent.GetEvent(guid)`：按事件 GUID 读取已加载事件。
- `DomainManager.TaiwuEvent.GetAllEventConfigs()`：列出已加载事件配置，用于有限搜索。
- `DomainManager.TaiwuEvent.ScriptRuntime`：事件脚本和条件运行时；解释时用于结构摘要和当前条件核对，选项脚本执行归写操作。

调用脚本时选 `targetSide = "backend"`、`entryThread = "mainThread"`。事件运行态依赖当前世界和主线程数据上下文；
纯文本整理可以在脚本内完成，但读取 `DomainManager.TaiwuEvent` 仍使用主线程入口。

## 可组合运行时原语

本节只提供可组合原语。入口外壳、`using`、返回值和工具参数处理由 `RUNTIME_SCRIPTING.md` 的脚本入口契约
统一说明；本节不重复脚本外壳。

读取当前显示事件：

```csharp
var data = DomainManager.TaiwuEvent.GetDisplayingEventData();
var current = data == null || string.IsNullOrEmpty(data.EventGuid)
    ? null
    : new
    {
        data.EventGuid,
        data.EventContent,
        Options = data.EventOptionInfos?.Select((option, index) => new
        {
            index,
            option.OptionContent,
            option.OptionState,
            option.OptionGuid,
            option.OptionKey,
            option.OptionConsumeInfos,
            option.OptionAvailableConditionInfos
        }).ToArray()
    };
```

读取待处理事件摘要：

```csharp
var pending = DomainManager.TaiwuEvent.GetTriggeredEventSummaryDisplayData()
    .Select((item, index) => new
    {
        index,
        item.EventGuid,
        item.CharacterId
    })
    .ToArray();
```

按 GUID 读取事件配置：

```csharp
var ev = DomainManager.TaiwuEvent.GetEvent(guid);
var config = ev?.EventConfig;
var info = config == null
    ? null
    : new
    {
        Guid = config.Guid.ToString(),
        config.EventGroup,
        config.IsHeadEvent,
        config.TriggerType,
        config.EventType,
        config.EventSortingOrder,
        config.EventContent,
        OptionCount = config.EventOptions?.Length ?? 0
    };
```

有限搜索已加载事件：

```csharp
var matches = DomainManager.TaiwuEvent.GetAllEventConfigs()
    .Where(config =>
        (config.EventGroup?.IndexOf(query, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
        || (config.EventContent?.Contains(query) ?? false)
        || (config.EventOptions?.Any(option => option.OptionContent?.Contains(query) == true) ?? false))
    .Take(20)
    .Select(config => new
    {
        Guid = config.Guid.ToString(),
        config.EventGroup,
        config.IsHeadEvent,
        config.TriggerType,
        config.EventType,
        OptionCount = config.EventOptions?.Length ?? 0,
        config.EventContent
    })
    .ToArray();
```

从当前事件定位选项并读取逻辑摘要：

```csharp
var showing = DomainManager.TaiwuEvent.ShowingEvent;
var option = showing?.EventConfig.EventOptions.FirstOrDefault(option =>
    option.OptionGuid == optionGuid || option.OptionKey == optionKey);

var optionInfo = option == null
    ? null
    : new
    {
        option.OptionContent,
        option.Behavior,
        option.DefaultState,
        option.OneTimeOnly,
        option.Important,
        option.IsVisible,
        option.IsAvailable,
        HasScript = option.Script != null,
        HasVisibleConditions = option.VisibleConditions?.Conditions != null,
        HasAvailableConditions = option.AvailableConditions?.Conditions != null,
        HasConfiguredConditions = option.OptionAvailableConditions != null,
        HasConsumes = option.OptionConsumeInfos != null,
        RedirectEventGuid = option.HasRedirect ? option.RedirectOption.EventGuid : null,
        RedirectOptionGuid = option.HasRedirect ? option.RedirectOption.OptionGuid : null
    };
```

读取脚本或条件结构摘要：

```csharp
var scriptSummary = option?.Script?.Instructions?.Select((inst, index) => new
{
    index,
    inst.Indent,
    inst.FunctionId,
    inst.AssignToVar
}).ToArray();

var visibleConditionSummary = option?.VisibleConditions?.Conditions?.Select((condition, index) => new
{
    index,
    condition.Indent,
    condition.FunctionId,
    condition.Reverse
}).ToArray();
```

只在当前事件已有 `ArgBox` 时求值条件：

```csharp
bool canEvaluate = showing != null && !showing.IsEmpty && showing.ArgBox != null;
bool? isVisible = canEvaluate && option != null ? option.IsVisible : null;
bool? isAvailable = canEvaluate && option != null ? option.IsAvailable : null;
```

## 数据解释

`TaiwuEventDisplayData` 是当前事件的玩家显示数据，常用字段：

- `EventGuid`：当前显示事件 GUID。
- `EventContent`：已经替换和解码后的事件正文，优先用于玩家答复。
- `EventOptionInfos`：当前可见选项。不可见选项不会出现在这里。
- `MainCharacter`、`TargetCharacter`、`ExtraData`：当前事件展示角色和扩展显示数据；只在玩家问题需要时读取。

`EventOptionInfo` 是当前可见选项的显示数据：

- `OptionContent`：玩家看到的选项文本。
- `OptionState`：`-1` 不可用，`0` 普通可选，`1` 未读，`2` 已读。
- `OptionGuid`、`OptionKey`：内部定位用；普通答复优先用选项文本或顺序。
- `OptionConsumeInfos`：选项消耗。
- `OptionAvailableConditionInfos`：脚本条件提示，包含事件函数 ID、参数和通过状态；不是所有条件都会提供可读提示。

`TaiwuEventSummaryDisplayData` 是待处理事件摘要：

- `EventGuid`：待处理事件 GUID。
- `CharacterId`：关联角色 ID；需要显示名时另查角色显示数据。

`Config.EventConfig.TaiwuEventItem` 是已加载事件配置：

- `Guid`、`EventGroup`、`IsHeadEvent`、`TriggerType`、`EventType`、`EventSortingOrder`。
- `MainRoleKey`、`TargetRoleKey`、`EventBackground`、`MaskControl`、`EscOptionKey`。
- `EventContent`：当前语言应用后的正文模板，可能仍含标签或占位。
- `EventOptions`：原始选项数组。
- `Script`：事件进入脚本；`Conditions`：事件触发/进入条件。

`Config.EventConfig.TaiwuEventOption` 是选项配置：

- `OptionKey`、`OptionGuid`、`OptionContent`、`Behavior`、`DefaultState`、`OneTimeOnly`、`Important`。
- `IsVisible`、`IsAvailable`：结合当前 `ArgBox` 和条件求值后的结果。
- `VisibleConditions`、`AvailableConditions`：脚本条件。
- `OptionAvailableConditions`、`OptionConsumeInfos`：配置式可用条件和消耗。
- `Script`：选中时执行的脚本；`RedirectOption`：重定向到其它事件选项。

`EventScript` 和 `EventConditionList` 适合做结构摘要。指令和条件稳定提供缩进、函数 ID、赋值变量、反转标记等
运行结构；需要把函数 ID 翻译成含义时，先返回 ID 和参数，再按当前问题做小范围定位。

## 可见性与显示

普通玩家问答优先使用 `TaiwuEventDisplayData`。它最接近玩家实际看到的正文、选项、可用状态和可读条件提示。
`EventConfig.EventContent`、`EventOption.OptionContent` 是运行时已加载语言后的模板，不等同于最终显示文本。

解释“为什么不可点”时，优先使用 `OptionState`、`OptionConsumeInfos` 和 `OptionAvailableConditionInfos`。这些仍不足时，
再说明当前运行时未给出可读原因，并补充静态结构，例如存在可用条件脚本、配置式条件或消耗。

解释“为什么看不到某选项”时，当前显示数据只能证明它未显示。若要追查原因，需要当前事件的 `EventOption` 和
`VisibleConditions`，并且只有在当前事件 `ArgBox` 存在时才能求值。

需要核对屏幕上的文本、按钮顺序或玩家是否真的处在某个事件窗口时，优先取得玩家视图；后端事件状态只作为权威状态
和原因补充。

## 写入边界

本文件只覆盖事件的检索和解释。选择事件选项、处理待处理事件、跳转事件或改变事件状态都属于写操作，
按 `RUNTIME_SCRIPTING.md` 的写操作规则处理；愿望、命令或强烈诉求涉及事件推进时，
仍按 `AGENTS.md` 的愿望回应规则先读取必要事实，再收束执行路径和影响范围。
