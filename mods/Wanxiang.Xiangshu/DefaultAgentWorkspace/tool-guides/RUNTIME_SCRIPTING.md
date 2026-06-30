# 运行时脚本指引

当前运行世界的权威状态、界面运行态或 Mod 运行状态会影响回答，或玩家目标需要读取、验证、执行、改动当前
游戏/界面/Mod 状态时，使用本文件。
玩家当前可见画面和可见结果按 `PLAYER_VIEW.md` 处理。

本文件描述相枢 Mod 脚本工具的运行时语义和入口契约。你从玩家目标判断是否需要脚本能力。使用前先确认当前
可用工具中存在脚本运行工具；工具名称、参数和副作用以当前可用工具说明为准。

## 运行工具边界

相枢 Mod 脚本能力通常包含：

- `xiangshu_run_csharp_script`：在相枢 Mod 前端或后端插件进程内运行完全受信的 C# 脚本。
- `xiangshu_send_intermediate_reply`：在当前回答完成前追加一条玩家可见的相枢中间答复。

脚本不是沙箱。会修改状态的代码会直接影响当前游戏进程。这里的脚本面向已加载游戏进程中的运行时对象；
本文件中的当前运行世界事实都指这些运行时对象；静态存档文件读写不属于默认脚本路径。目标、影响和玩家意图不清楚时，
先运行最小只读查询。仍辨不清落点时，按 `AGENTS.md` 的本回合路由和愿望回应规则收束。本文件只补充脚本侧边界：
API、类型、目标侧或脚本形态属于脚本路径内部选择，优先用只读探测、领域指引和游戏知识入口自行收窄。

本文件只解释脚本侧语义。工具注册、传输和可用性由相枢 Mod 运行时和当前可用工具决定。

## 脚本职责

写脚本前先确定玩家目标对应的游戏领域、事实来源和最小结果。脚本只承担当前请求必要的读取、验证或写入；
脚本主体按当前请求组织。Agent 工作区提供可跨任务复用的入口层级、接续点和局部处理范式；当前脚本负责
把它们组合成面向玩家目标的读取、处理和输出路径。

当前脚本可以自然包含过滤、投影、排序、分组、分页、名称补全和结果裁剪等数据流处理；这些处理应服务当前
玩家目标和当前返回事实。数据结构不确定时，用当前目标相关的小范围只读探测返回类型、成员或少量样本，再据此
写当前脚本；探测结果只作为当前脚本依据，未验证的候选不要写成玩家可见事实。

领域指引可以保留少量参考片段，用来固定高频入口、线程选择、必要 `using`、返回事实和常见空值边界。
片段只承担“从哪里下手、能接到哪里”的作用；使用时按当前请求删减、改名和收窄返回值，组合片段时以当前
请求的输入、输出和副作用边界收束。

## 运行环境

脚本在目标插件进程内编译并执行，不在独立 sidecar 进程里执行。目标侧影响可引用程序集、可访问的游戏 API、
线程边界和可见运行状态。

- `frontend`：前端插件侧，项目目标框架是 `netstandard2.1`。用于前端 UI、聊天窗口、热键、Unity 对象、当前
  界面和前端运行态。若问题只需要玩家当前看见的画面，先按 `PLAYER_VIEW.md` 取得玩家可见证据；脚本用于
  定位、读取目标 UI 状态或补充验证。
- `backend`：后端插件侧，项目目标框架是 `net8.0`。用于后端游戏运行数据、世界/角色/物品/地图等后端
  API 和后端运行态。
- 共享脚本运行库同时面向 `netstandard2.1` 和 `net8.0`，因此同一入口契约可被前后端复用。
- 外部工具服务进程不是脚本运行目标；它只负责把工具请求转给前端或后端。

脚本工具的 `entryThread` 参数控制入口调用线程：

- `current`：默认值，在 IPC 处理线程调用入口。用于纯计算、程序集/类型探测和格式整理等不依赖游戏运行态的
  脚本。
- `mainThread`：在目标侧游戏主线程调用入口。访问 Unity 对象、前端 UI、后端 `DomainManager` 数据域、游戏实体或修改
  运行状态时使用。

`entryThread` 只控制入口调用；入口内部自行创建任务或调用异步/回调 API 后，继续按对应 API 的线程语义运行。

目标侧不确定，或需要查询游戏机制、配置、本地化、模板/显示辅助、百晓册资料或主要 namespace 时，先读取
`GAME_KNOWLEDGE.md`，按事实来源和命名空间地图缩小脚本范围。目标侧探测使用无副作用脚本。

## 脚本入口

脚本内容是完整 C# 编译单元。脚本必须声明满足入口契约的 namespace 和类型；其余 `using`、辅助类型和返回值结构
按当前请求组织。本节只保留入口契约骨架。
领域指引只给事实入口和边界；实际脚本按当前请求生成最小主体，按需要组合指引里的锚点和参考片段。

```csharp
using System.Threading.Tasks;

namespace Wanxiang.Xiangshu.Scripting;

public static class XiangshuScript
{
    public static Task<object?> ExecuteAsync(XiangshuScriptGlobals globals)
    {
        return Task.FromResult<object?>(null);
    }
}
```

入口类型完整名必须是 `Wanxiang.Xiangshu.Scripting.XiangshuScript`，且必须是 `public static` 非泛型 class。
入口方法是 `Execute` 或 `ExecuteAsync`，参数必须是 `Wanxiang.Xiangshu.Scripting.XiangshuScriptGlobals`。
同步返回值、`Task` 和 `Task<T>` 都可以作为入口返回；需要异步时优先让入口返回 `Task<object?>`。脚本需要的
其它命名空间通过 `using` 显式声明。

主线程需求由工具调用层承担：访问 Unity 对象、前端 UI、后端 `DomainManager` 数据域、游戏实体或运行状态时，
调用脚本工具选择 `entryThread = "mainThread"`。入口内部继续创建任务或调用异步/回调 API 时，才按对应 API 的
线程语义处理；入口线程选择仍以工具参数为准。

`globals` 只提供：

- `Side`：当前目标侧。
- `Arguments`：由工具参数传入的字符串字典；非字符串 JSON 值通常会以紧凑 JSON 字符串传入。
- `CancellationToken`：当前脚本调用的取消信号。

`globals` 的公共成员只有以上三项；游戏 facade、服务容器和命名空间声明由脚本或目标侧 API 自行处理。
宿主会提供相枢脚本契约引用，并按目标侧显式开放少量能力引用；不会把插件目录中的 DLL 全量作为脚本编译引用。

## 结果判断

脚本工具返回的是运行事实，不替你判断玩家目标是否达成。常见形态：

- `notInvoked(reason, details?)`：入口未执行，常见原因是编译失败、引用失败、入口契约不满足或调用前取消。
  编译阶段未产出程序集时，`details.referenceDiagnostics` 保留宿主引用设置问题，`details.compilationDiagnostics`
  保留 Roslyn 原始诊断。
- `invoked(returnValue)`：入口已执行并返回。
- `invoked(exception)`：入口已执行但抛出异常。

普通玩家答复只呈现可理解的相枢文本。玩家明确询问本机链路调试或 Mod 开发时，再解释内部结果格式。

## 运行时锚点

下面是写脚本时的常见入口锚点，用来提供游戏领域上下文，不依赖任何工具传输知识，也不是完整 API 清单或
查询体预设。写脚本时先用类型和方法层级入口缩小范围；返回对象形态按目标插件进程的当前结果确认。

后端常见锚点：

- `GameData.Domains.DomainManager`：后端域集中入口。常见域包括 `Taiwu`、`Character`、`Item`、`Map`、`World`、
  `Organization`、`Building`、`CombatSkill`、`Combat`、`Adventure`、`TaiwuEvent`、`LifeRecord`、`Information`、
  `LegendaryBook`、`Merchant`、`Mod`、`Global`、`Extra`、`Story`、`TutorialChapter` 和 `SpecialEffect`。
- `GameData.Domains.*`：后端数据域、实体和显示数据类型。
- `Config.*`：配置表与配置项；读取形态由当前表类型和查询方法确认。
- `GameData.GameDataBridge`、`GameData.Utilities`、`GameData.Common`：跨端消息、工具类型、集合和值对象。

前端常见锚点：

- `Game.Views.*`、`Game.Components.*`：界面视图和 UI 组件。
- `Game.Views.Encyclopedia.*`、`Game.Views.Encyclopedia.Views.*`：百晓册数据、搜索和面板入口；检索策略见
  `GAME_KNOWLEDGE.md`。
- `FrameWork.UISystem`、`FrameWork.ModSystem`、`Game.CommandSystem`、`UICommon`：前端 UI、mod 和命令系统。
- `GameData.GameDataBridge`、`GameData.GameDataBridge.UnityAdapter`、`GameDataExtensions`：前后端数据桥接和
  前端显示辅助。
- `Config.*`、`LocalStringManager`、`BasicGameData`、`UIManager`、`SingletonObject.getInstance<T>()`
  是常见检索锚点；返回对象形态按当前结果确认。

## 当前事实锚点

下面是读取当前运行世界或界面运行事实时的直接路线。它们不是完整 API 清单，只用于减少无目标反射。玩家可见
画面事实归 `PLAYER_VIEW.md`；这些锚点用于补充权威状态、目标对象身份和定位辅助。

后端当前运行世界：

- `DomainManager.Taiwu.GetTaiwuCharId()`、`DomainManager.Taiwu.GetTaiwu()`：当前太吾角色入口。
- `DomainManager.Character.GetElement_Objects(charId)`、`TryGetElement_Objects(...)`：角色实体入口。
- `DomainManager.Taiwu.GetItems(itemSourceType)`、`DomainManager.Taiwu.GetTaiwuAllItems(context)`：太吾物品来源与汇总。
- `DomainManager.Item.GetItemDisplayData(...)`、`GetItemDisplayDataListOptional(...)`：把物品实例整理为显示数据。
- `DomainManager.Item.GetElement_*`、`TryGetElement_*`：按目标物品实体类型和实例 id 读取物品；类型不确定时先用
  `ItemKey`、`ItemDisplayData` 或模板辅助缩小范围。
- `DomainManager.Map.GetBlock(location)`、`TryGetBlock(...)`、`GetAreaByAreaId(...)`、`GetAreaBlocks(...)`：地图和区域事实。
- `DataContextManager.GetCurrentThreadDataContext()`：后端主线程脚本里取得当前 `DataContext`。调用带 `DataContext`
  参数的后端只读域方法时优先使用它。
- `DomainManager.TaiwuEvent.GetDisplayingEventData()`、`GetTriggeredEventSummaryDisplayData()`、`GetEvent(guid)`、
  `GetAllEventConfigs()`：事件领域方法锚点；需要当前事件、事件节点、选项、条件、跳转或可能触发候选时先看
  `EVENTS.md`，本文件只承担脚本入口和线程规则。
- `DomainManager.LifeRecord.GetReversedRecord(context, charId, startCount, readCount)`：角色经历分页读取原语；需要
  最近经历、生平事件、经历分类、按经历找角色、梦回经历或可见性边界时先看 `LIFE_RECORDS.md`，本文件只承担脚本入口和线程规则。

前端当前界面：

- `UIManager`、`UIElement`：界面显示和打开状态入口。
- `SingletonObject.getInstance<WorldMapModel>()`：世界地图前端模型；位置、区块和区域名称由当前返回对象或相关方法确认。
- `BasicGameData` 和 `GameData.GameDataBridge.UnityAdapter`：前端缓存、桥接和显示态辅助。

读取当前事实时，先用明确域和 id；不知道 id 时，先读取最小上下文（例如太吾 id、当前位置或当前 UI），再继续查。

## 使用策略

- 先按事实归属选择观察方式，再决定是否修改；玩家可见问题先读 `PLAYER_VIEW.md`，权威状态问题再读运行时对象。
- 访问 Unity UI/对象、后端游戏域或运行时实体时使用 `mainThread`；纯计算或类型探测保留 `current`。
- 查询游戏知识、目标侧或 namespace 时，先走 `GAME_KNOWLEDGE.md` 中的命名空间地图、配置、本地化、模板/显示辅助和
  百晓册入口；反射只用于定位缺失类型或方法。
- 让脚本只返回回答、验证或下一步判断需要的紧凑结构化事实，再把结果转成玩家能理解的答复。
- 编译失败时，先看 `details.referenceDiagnostics` 和 `details.compilationDiagnostics`，再收窄 using、类型名和目标侧。
- 工具不可用、检索失败或脚本探索失败时，先收窄问题、换用只读路径，或基于已知事实给出清楚答复。
- 长任务需要先让玩家看见进展时，可以发送中间答复；中间答复必须已经是玩家可见的相枢文本，不暴露工具、
  进程或错误栈。
