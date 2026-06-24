# GM 与调试入口指引

当前目标需要定位、解释、比较或借用游戏内预置 GM/调试入口，或需要借这些入口寻找比直接写运行时对象更接近游戏已有
逻辑的状态改动路径时，读取本文件。普通游戏机制查询仍先走 `GAME_KNOWLEDGE.md`；当前事件、事件选项和事件脚本结构
仍先走 `EVENTS.md`；实际脚本入口、目标侧、线程规则和写操作边界仍以 `RUNTIME_SCRIPTING.md` 为准。

本文件只提供定位路线、边界和少量高频锚点。GM/调试入口由当前游戏进程中加载的 UI、命令行、方法和事件配置拥有；
完整清单、参数和具体实现以当前运行时事实为准。它们是 Agent 可借用的内部实现路线，不是玩家意图分类，也不是
玩家可见玩法契约；普通玩家答复仍只呈现相枢可见的结果、路径或代价。

## 查询模型

先区分当前目标涉及的 GM/调试入口属于哪一类：

- GM 面板与命令行：前端 `UI_GMWindow` 加载带 `GMMemberAttribute` 的成员，内置主体在 `GMFunc`，额外类型可来自
  `UI_GMWindow.GMFuncTypes`。函数按钮来自 `GMFuncAttribute`，命令行名称来自 `GMFuncAttribute.ConsoleName`
  或方法名转 snake_case。
- 事件里的 GM 风格选项：某些事件配置带有文本含 `GM`、`GM：` 或 `作弊` 的选项，可能配有永假可见条件；先按测试
  或编辑器痕迹处理，是否当前可见以显示数据和可见条件为准。它们归 `TaiwuEvent` 事件配置和事件脚本结构，不等同于
  GM 面板命令。

两类入口都可能导向写操作，改变当前运行世界、界面运行态或调试开关。只读检索可以直接做；执行、模拟执行、跳转事件、
改变量、给物品、改人物或跳过流程都按写操作处理。需要改变状态时，本文件用于比较后端域 API、已有 GM/调试入口
和直接运行时改动的所有权、参数边界和验证路径。

## GM 面板路线

GM 面板是前端调试 UI：

- 启用条件：`GameApp` 读取命令行参数 `--enable-gm` 后设置 `EnableGMPanel`；若游戏数据目录下存在
  `disable_cmd_args.txt`，命令行参数会被跳过。
- 打开方式：通用热键 `CommonCommandKit.OpenGMPanel` 默认是反引号 `BackQuote`；热键只在 `EnableGMPanel`
  条件下进入系统设置显示。
- 生命周期：`GameStateInGame` 调用 `UI_GMWindow.EnsureInstanceExist(GameApp.Instance.EnableGMPanel)`；
  `UI_GMWindow.ValidGMWindow()` 要求当前游戏状态是 `InGame`。
- 页面分组：`EGMGroup` 映射到 `EGMPage`，常见页包括角色、地图、战斗、产业、功法、技艺、世界功能、奇遇、
  大事件和杂项。
- 命令行：`GMCommandLine` 为所有 `GMFuncAttribute` 方法建立命令名，支持补全和历史；参数按方法签名解析，
  特殊变量 `$taiwu_id` 会解析为当前太吾角色 ID。
- UI 按钮限制：`UI_GMWindow.GetGMFunc` 在旅行中且不在奇遇内时会拒绝调用，并提示不能在旅行中使用 GM 命令。
  这是按钮路径的限制；命令行和直接反射调用仍需按目标方法、当前状态和写操作边界另行判断。

需要确认玩家是否能看见或操作 GM 面板时，先按 `PLAYER_VIEW.md` 取得玩家可见证据；需要枚举、解释、比较或借用
命令路径时，在 `frontend` 目标侧读取 GM 类型和本地化；需要改后端世界状态时，优先确认当前世界事实，再决定使用
稳定后端域 API、调用对应 GM 方法，还是只把 GM 实现当作参考来组织更窄的状态改动。

## 事件 GM 选项路线

事件里的 GM 风格选项优先按 `EVENTS.md` 检索：

1. 当前窗口问题先读 `DomainManager.TaiwuEvent.GetDisplayingEventData()`，确认玩家是否真的看见了 GM 文本。
2. 非当前窗口的检索用 `DomainManager.TaiwuEvent.GetAllEventConfigs()`，按选项文本 `GM`、`GM：`、`作弊`、事件组或
   正文片段找候选。
3. 命中后读取目标事件的 `EventOptions`、选项 GUID、`OptionVisibleConditionList` 和 `OptionScript`。
4. 如果可见条件是永假表达式，把结论限定为“配置存在但不是当前正常显示入口”。普通问答只解释结构事实；
   执行等价状态改动属于写操作；当前目标需要跳过、生成、修改或调试状态时，先核对当前事件、奇遇或地图上下文，
   再决定是否借用该选项脚本、复用同一状态改动，或停在结构解释。

## 参考片段

这些片段只展示当前领域的核心调用主体，不重复 `RUNTIME_SCRIPTING.md` 的入口外壳。使用时按当前请求删减返回字段，
并把所列命名空间放到脚本顶部。片段可以作为 GM 面板、命令行和事件 GM 选项的只读检索接续点；组合片段时，
以当前问题的输入、输出和副作用边界收束。
片段中的过滤、投影和限量返回是为了展示调试入口的稳定取法和最小可读形状；当前脚本可以按玩家问题调整这些
处理步骤。

### GM 命令枚举

前提：`frontend`、`mainThread`。需要 `System`、`System.Linq`、`System.Reflection`、`System.Text`、`GM`。
输入：可选 `globals.Arguments["query"]`。
输出：有限命令候选的类型、方法名、命令行名称、显示名、页面、分组、运行模式、参数名、参数类型和控件类型。
可接续：命令行名称可用于解释 GM 控制台用法；方法名和类型名可用于继续读取实现，或在当前目标确实需要写操作时
比较其与后端域 API、直接状态改动的边界。

```csharp
string ToSnakeCase(string value)
{
    var builder = new StringBuilder();
    foreach (char ch in value)
    {
        if (char.IsUpper(ch))
        {
            if (builder.Length > 0)
            {
                builder.Append('_');
            }
            builder.Append(char.ToLowerInvariant(ch));
        }
        else
        {
            builder.Append(ch);
        }
    }
    return builder.ToString();
}

string GetDisplayName(string memberName)
{
    return Enum.TryParse($"GM_{memberName}_Name", out LanguageKey key) && key != LanguageKey.Invalid
        ? LocalStringManager.Get(key)
        : memberName;
}

string query = globals.Arguments.TryGetValue("query", out string value) ? value : "";
var types = typeof(UI_GMWindow).Assembly.GetTypes().Concat(UI_GMWindow.GMFuncTypes);

var matches = types
    .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        .Select(method => new
        {
            TypeName = type.FullName,
            Method = method,
            Attribute = method.GetCustomAttribute<GMFuncAttribute>()
        }))
    .Where(item => item.Attribute != null)
    .Select(item =>
    {
        string displayName = GetDisplayName(item.Method.Name);
        string command = item.Attribute!.ConsoleName ?? ToSnakeCase(item.Method.Name);
        var argAttrs = item.Method.GetCustomAttributes<GMFuncArgAttribute>()
            .ToDictionary(attr => attr.Index);

        return new
        {
            item.TypeName,
            MethodName = item.Method.Name,
            Command = command,
            DisplayName = displayName,
            Group = item.Attribute.Group.ToString(),
            Page = GMGroupToPage.GetPage(item.Attribute.Group).ToString(),
            RunMode = item.Attribute.RunMode.ToString(),
            Parameters = item.Method.GetParameters().Select((parameter, index) => new
            {
                parameter.Name,
                Type = parameter.ParameterType.Name,
                Optional = parameter.IsOptional,
                Widget = argAttrs.TryGetValue(index, out GMFuncArgAttribute argAttr)
                    ? argAttr.WidgetType.ToString()
                    : "Auto"
            }).ToArray()
        };
    })
    .Where(item =>
        string.IsNullOrEmpty(query)
        || item.MethodName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
        || item.Command.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
        || item.DisplayName.Contains(query)
        || item.Group.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
        || item.Page.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
    .Take(30)
    .ToArray();

return new { query, matches };
```

这个片段只枚举 `GMFuncAttribute` 方法。GM 面板里的属性开关和对象块来自 `GMPropertyAttribute`、`GMObjectAttribute`；
当前目标涉及开关状态、面板页或对象块时，再按 `GMMemberAttribute` 扩展枚举范围。

### 事件 GM 选项搜索

前提：`backend`、`mainThread`。需要 `System`、`System.Linq`、`GameData.Domains`。
输入：可选 `globals.Arguments["query"]`，默认搜索 `GM`。
输出：有限候选的事件 GUID、事件组、选项顺序、选项 GUID 和选项文本。
可接续：候选事件 GUID 和选项 GUID 可交给 `EVENTS.md` 中的节点与选项结构解析；输出只证明配置可检索，不证明
选项当前显示、可选或适合执行。

```csharp
string query = globals.Arguments.TryGetValue("query", out string value) ? value : "GM";
string[] terms = query == "GM" ? new[] { "GM", "GM：", "作弊" } : new[] { query };

var matches = DomainManager.TaiwuEvent.GetAllEventConfigs()
    .SelectMany(config => (config.EventOptions ?? Array.Empty<Config.EventConfig.TaiwuEventOption>())
        .Select((option, index) => new
        {
            EventGuid = config.Guid.ToString(),
            config.EventGroup,
            OptionIndex = index,
            option.OptionGuid,
            option.OptionContent
        }))
    .Where(item => terms.Any(term =>
        item.OptionContent?.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
    .Take(30)
    .ToArray();

return new { query, matches };
```

## 已知锚点

九似真藏奇遇里有两个隐藏 GM 风格选项，可作为事件检索和脚本解析样例：

- 事件组：`Taiwu_EventPackage_Adventure_StrangeBookAdventure_JiuSiZhenCang`。
- 事件 GUID：`9e92b875-6f68-4a39-a7fa-c0c8ac6f2fdc`。
- 选项 6：`GM：跳过第一阶段石门`，选项 GUID `8af98fa8-afb6-4fad-938f-16c5a6393ad5`。脚本会隐藏云组 `1`，
  设置奇遇参数 `over1 = 1`，并让太吾方向移动 `(0, 10)`。
- 选项 7：`GM：1000游龙劲`，选项 GUID `32646c72-1532-4e03-91a5-f7f0ba2de65d`。脚本会设置奇遇参数
  `ylj = 1000`。
- 两个选项的可见条件都是 `CheckExpression false`，说明配置可检索，但它们不是当前正常显示入口。

这个锚点只证明当前快照中存在这两个预置调试选项，不代表 GM 选项清单完整。其它事件包可能也有测试痕迹；
需要时按事件配置库和事件脚本重新检索。

## 执行边界

- 把 GM 能力当作游戏已有实现的索引和可选执行路径。能用明确后端域 API 完整表达的当前世界
  改动，通常直接用后端域 API 更可控。若目标正好对应已有 GM/调试方法或事件 GM 脚本，先读其实现和参数边界，
  再决定调用该入口、复用同一状态改动，或停在解释。直接写运行时字段或对象只用于没有更合适入口，且已经确认
  目标、影响和验证路径的窄范围情形。
- 直接调用 `GMFunc` 方法属于前端写操作，很多方法会走 UI、前端模型、异步桥接或全局静态开关。调用前先确认当前
  游戏状态、目标角色/物品/地点和玩家意图。
- 事件 GM 选项的“等价执行”不一定等同于点击选项；选项可能依赖当前 `ArgBox`、奇遇变量、当前事件、镜头或地图状态。
  当前事件不存在或上下文不匹配时，先只解释结构，或改用更窄的状态改动。
- 玩家只问“有没有 GM 指令、它们是什么”时，停在只读枚举和解释。当前目标需要使用 GM、跳过、生成、修改或调试
  状态时，再按 `RUNTIME_SCRIPTING.md` 的脚本边界执行，并在执行后验证目标结果。
