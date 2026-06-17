# 运行时脚本指引

当前游戏、存档、界面或 mod 运行状态会影响回答，或玩家明确要求执行操作时，才使用本文件。普通闲谈、
静态世界观解释和创作请求直接回答。

本文件描述默认相枢脚本工具的运行时语义。使用前先确认当前可用工具中存在脚本运行工具；工具名称、参数和副作用以
当前可用工具说明为准。

## 运行工具边界

默认相枢脚本能力通常包含：

- `xiangshu_run_csharp_script`：在相枢前端或后端插件进程内运行完全受信的 C# 脚本。
- `xiangshu_send_intermediate_reply`：在当前回答完成前追加一条玩家可见的相枢中间答复。

脚本不是沙箱。会修改状态的代码会直接影响当前游戏进程。目标、影响和玩家意图不清楚时，先运行最小只读
查询；仍不清楚且存在写操作风险时，保持只读并提出必要澄清。信息不足但能安全回答时，直接说明边界并
给可用建议。

本文件只解释脚本侧语义。工具注册、传输和可用性由相枢运行时和当前可用工具决定。

## 运行环境

脚本在目标插件进程内编译并执行，不在独立 sidecar 进程里执行。目标侧影响可引用程序集、可访问的游戏 API、
线程边界和可见运行状态。

- `frontend`：前端插件侧，项目目标框架是 `netstandard2.1`。用于前端 UI、聊天窗口、热键、Unity 对象、当前
  界面和前端运行态。
- `backend`：后端插件侧，项目目标框架是 `net8.0`。用于后端游戏数据、存档域、世界/角色/物品/地图等后端
  API 和后端运行态。
- 共享脚本运行库同时面向 `netstandard2.1` 和 `net8.0`，因此同一入口契约可被前后端复用。
- 外部工具服务进程不是脚本运行目标；它只负责把工具请求转给前端或后端。

目标侧不确定时，先在最可能的一侧运行只读探测。目标侧探测使用无副作用脚本。
需要查询游戏机制、配置、本地化、模板/显示辅助或百晓册资料时，先读取 `GAME_KNOWLEDGE.md`，按直接入口缩小脚本范围。

## 脚本入口

脚本内容是完整 C# 编译单元，不是片段。脚本需要自己声明 `using`、namespace、类型和返回值。

```csharp
using Wanxiang.Xiangshu.Scripting;

public static class XiangshuScript
{
    public static object? Execute(XiangshuScriptGlobals globals)
    {
        return new { side = globals.Side };
    }
}
```

入口类型的简单名必须是 `XiangshuScript`，且必须是 `public static` 非泛型 class。入口方法是 `Execute` 或
`ExecuteAsync`，参数必须是 `Wanxiang.Xiangshu.Scripting.XiangshuScriptGlobals`。同步返回值、`Task` 和
`Task<T>` 都可以作为入口返回。

`globals` 只提供：

- `Side`：当前目标侧。
- `Arguments`：由工具参数传入的字符串字典；非字符串 JSON 值通常会以紧凑 JSON 字符串传入。
- `CancellationToken`：当前脚本调用的取消信号。

`globals` 不提供游戏 facade、服务容器或预置 using 列表。

## 结果判断

脚本工具返回的是运行事实，不替你判断玩家目标是否达成。常见形态：

- `notInvoked(reason)`：入口未执行，常见原因是编译失败、引用失败、入口契约不满足或调用前取消。
- `invoked(returnValue)`：入口已执行并返回。
- `invoked(exception)`：入口已执行但抛出异常。

普通玩家答复只呈现可理解的相枢文本。玩家明确询问本机调试或 mod 开发时，再解释内部结果格式。

## 运行时锚点

下面是写脚本时的常见入口锚点，不依赖任何工具传输知识，也不是完整 API 清单。实际可用成员以目标插件
进程已加载、可引用的程序集为准。

后端常见锚点：

- `GameData.Domains.DomainManager`：后端域集中入口。常见域包括 `World`、`Map`、`Organization`、
  `Character`、`Taiwu`、`Item`、`CombatSkill`、`Combat`、`Building`、`Adventure`、`LegendaryBook`、
  `TaiwuEvent`、`LifeRecord`、`Merchant`、`TutorialChapter`、`Mod`、`SpecialEffect`、`Information`、`Extra`、
  `Story`。
- `GameData.Domains.*`：后端数据域、实体和显示数据类型。
- `Config.*`：配置表与配置项，常见访问形态是 `Config.SomeTable.Instance[...]`。
- `GameData.GameDataBridge`、`GameData.Utilities`、`GameData.Common`：跨端消息、工具类型、集合和值对象。

前端常见锚点：

- `Game.Views.*`、`Game.Components.*`：界面视图和 UI 组件。
- `Game.Views.Encyclopedia.*`、`Game.Views.Encyclopedia.Views.*`：百晓册数据、搜索和面板入口；具体检索策略见
  `GAME_KNOWLEDGE.md`。
- `FrameWork.UISystem`、`FrameWork.ModSystem`、`Game.CommandSystem`、`UICommon`：前端 UI、mod 和命令系统。
- `GameData.GameDataBridge`、`GameData.GameDataBridge.UnityAdapter`、`GameDataExtensions`：前后端数据桥接和
  前端显示辅助。
- `Config.*`、`LocalStringManager`、`BasicGameData`、`UIManager.Instance`、`SingletonObject.getInstance<T>()`
  是常见检索锚点；实际成员以运行时可访问类型为准。

## 当前事实锚点

下面是读取当前存档或界面事实时的直接路线。它们不是完整 API 清单，只用于减少无目标反射。

后端当前存档：

- `DomainManager.Taiwu.GetTaiwuCharId()`、`DomainManager.Taiwu.GetTaiwu()`：当前太吾角色入口。
- `DomainManager.Character.GetElement_Objects(charId)`、`TryGetElement_Objects(...)`：角色实体入口。
- `DomainManager.Taiwu.GetItems(itemSourceType)`、`DomainManager.Taiwu.GetTaiwuAllItems(context)`：太吾物品来源与汇总。
- `DomainManager.Item.GetItemDisplayData(...)`、`GetItemDisplayDataListOptional(...)`：把物品实例整理为显示数据。
- `DomainManager.Item.GetElement_*`、`TryGetElement_*`：按具体物品实体类型和实例 id 读取物品；类型不确定时先用
  `ItemKey`、`ItemDisplayData` 或模板辅助缩小范围。
- `DomainManager.Map.GetBlock(location)`、`TryGetBlock(...)`、`GetAreaByAreaId(...)`、`GetAreaBlocks(...)`：地图和区域事实。

前端当前界面：

- `UIManager.Instance`、`UIElement.*`：界面显示和打开状态入口。
- `SingletonObject.getInstance<WorldMapModel>()`：世界地图前端模型，常见事实包括 `CurrentLocation`、`TryGetBlockData(...)`、
  `GetAreaName(...)`、`GetTaiwuVillageBlock()`。
- `BasicGameData` 和 `GameData.GameDataBridge.UnityAdapter`：前端缓存、桥接和显示态辅助。

读取当前事实时，先用具体域和具体 id；不知道 id 时，先读取最小上下文（例如太吾 id、当前位置或当前 UI），再继续查。

## 使用策略

- 先读当前事实，再决定是否修改。
- 查询游戏知识时，先走 `GAME_KNOWLEDGE.md` 中的配置、本地化、模板/显示辅助和百晓册入口；反射只用于定位缺失成员。
- 让脚本返回紧凑、结构化的数据，再把结果转成玩家能理解的答复。
- 编译失败时，依据 `reason` 收窄 using、类型名和目标侧。
- 工具不可用、检索失败或脚本探索失败时，先收窄问题、换用只读路径，或基于已知事实给出边界清楚的答复。
- 长任务需要先让玩家看见进展时，可以发送中间答复；中间答复必须已经是玩家可见的相枢文本，不暴露工具、
  进程或错误栈。
