# 法不加身设计说明

本文记录法不加身的运行时设计。玩家说明见 [../README.md](../README.md)，维护入口见 [../DEVELOPMENT.md](../DEVELOPMENT.md)。

## 目标

法不加身只在运行时改变太吾面对伤害、标记和毒素时的规则表现，不新增真实人物“特性”、不保存额外人物数据、不改游戏配置表。前端只在太吾人物特性列表显示“法不加身”标记，用于提示这些运行时规则正在作用于太吾；该标记不是存档中的人物特性。目标行为分为以下几类：

- 太吾受到的内伤、内伤进度、心神伤害和心神标记无效。
- 太吾在人物免疫查询层呈现内伤、心神、破绽和封穴免疫；进入战斗后同步塑形战斗角色。
- 太吾在战斗中以毒抗数值表达毒素免疫；战斗外新增毒素写入无效。
- 太吾战斗外新增内伤无效，但不会把被拦截的内伤补偿为外伤。
- 太吾不接受新的战斗状态增益、减益或特殊状态；太吾作为来源时，不改变别人的这类状态。
- 太吾输出的内伤、心神伤害和相关标记无效，不补偿为外伤。
- 太吾不会受到或造成直接施加的致命伤、致命标记和致命标记转移；外伤满溢转化的致命伤视为普通外伤流程保留。
- 太吾参与战斗时，施展和入战触发的功法特殊效果注册判定跳过太吾；属性修正枚举阻断太吾相关的施展和入战特效及跨角色被动修正，但保留太吾自身装备或突破被动、真气、功法配置和普通外伤等基础计算。

## 三层结构

实现分为“战斗角色塑形”、“公共入口拦截”和“真实人物规则兜底”三层。

源码上，`FabujiashenRules` 承载这些规则和边界判断；`FabujiashenPatches` 只负责安装 Harmony patch、绑定游戏入口，并调用规则模型。这样设计说明里的三层结构不依赖单个 patch 类的排列顺序。

### 战斗角色塑形

战斗开始时，游戏会为参战人物创建 `CombatCharacter` 并放入战斗域字典；战斗结束结算后再移除。法不加身在 `CombatCharacter.Init` 内完成游戏字段初始化和 `StateMachine.Init` 之后、初始 `StateMachine.TranslateState` 之前，只塑形这个战斗角色对象：

- 通过 `CombatDomain.SetDefeatMarkImmunity` 设置内伤、心神、破绽和封穴的战斗角色免疫，并保留太吾已有的外伤免疫表现。
- 将战斗角色毒素抵抗提高到游戏定义的免疫阈值 `GlobalConfig.MaxPoisonResistance`，毒素类型数量取 `PoisonType.Count`。

这层的目的不是替代伤害落点，而是让功法判定和显示查询尽早读到同一套规则。后续游戏入口若重设战斗角色免疫或毒抗，也会在对应入口保持太吾规则。当前版本不改太吾功法、武器或战斗上下文中的内外伤比例。

战斗角色塑形不改 `DamageStepCollection`。游戏没有暴露“内伤/心神免疫阈值”这类语义常量，而实际伤害计算会先读取免疫 flag，再进入伤害阈值换算；因此这里不使用自造的大数哨兵来抬高内伤和心神 damage step。战斗角色塑形也不主动清理进入战斗前已有的伤势、破绽、封穴、毒素或伤害进度。

### 公共入口拦截

公共入口拦截用于覆盖战斗中的直接调用、特殊效果绕路和战斗角色塑形无法表达的非外伤输出。当前 patch 点覆盖以下入口：

- 太吾作为防御方时，内伤、心神伤害和标记入口直接无效。
- 太吾作为攻击方时，混合伤害中的内伤结果清零，不并入外伤。
- 太吾作为攻击方的心神伤害直接跳过，不定位部位、不补偿外伤。
- `AddInjuryDamageValue` 这类直接伤害值入口把太吾输出的内伤值清零，外伤值不增加。
- `AddPoison` 公共战斗入口跳过太吾作为防御方或攻击方的新增毒素，防止毒抗被特殊效果临时拉低后绕过战斗角色塑形。
- `AddCombatState` 公共战斗状态入口跳过太吾作为目标的正向新增或叠加；当 `srcCharId` 可见且来源是太吾，或当前执行的特殊效果持有人是战斗中的太吾时，跳过该来源造成的状态变化。
- `AddFatalDamage`、`AddFatalMark`、`AddFatalMarkImmediate` 和 `TransferFatalMark` 阻止太吾被直接增加致命伤进度或致命标记，也阻止太吾作为来源造成这类直接效果；`AddInjuryDamageValue` 和 `ApplyMixedInjury` 内的外伤满溢调用通过显式包装放行。
- 战斗功法特殊效果注册只拦截 `Cast` 和 `EnterCombat`。`Custom`、`Add`、`AddPercent` 和 `TotalPercent` 修改枚举在太吾参战时过滤 `Cast`、`EnterCombat` 和跨角色的 `Equipped`、`Broken` 功法特殊效果；太吾自身装备或突破被动对太吾自己的数值修正保留。这两类入口分别覆盖功法触发特效注册和数值修正，不依赖战斗状态 `srcCharId` 归因。

### 真实人物规则兜底

游戏源码中，`Character.GetInnerInjuryImmunity`、`GetMindImmunity`、`GetFlawImmunity` 和 `GetAcupointImmunity` 只读取人物配置表标记；真实人物内伤写入入口 `ChangeInjury`、`ChangeInjuries` 和 `SetInjuries` 也不会调用这些 getter。因此战斗外的免疫规则不能只改查询结果，还需要在真实人物写入层兜底：

- `GameCharacter` 的内伤、心神、破绽和封穴免疫 getter 对太吾返回 `true`，让战斗外事件和显示查询能看到同一套免疫语义。
- `SetInjuries` 写回真实人物伤势时保留太吾已有内伤，只阻止新增内伤值；清伤和减伤仍交回游戏。
- `ChangeInjury`、`ChangeInjuries`、`TakeDamageRandomParts`、`TakeRandomDamage(context, damage, isInnerInjury)` 拦截或清零太吾的正向内伤增量。
- `TakeRandomDamage(context, damage)` 仍按游戏原本 14 个内外伤槽抽取；抽到外伤正常增加，抽到内伤则本次无效，避免把免疫掉的内伤折算成外伤。
- 按值写回的 `SetPoisoned` 保留太吾已有毒素，只阻止任一毒素类型升高；解毒和降毒仍交回游戏。
- `ChangePoisoned` 两个重载、`DirectlyChangePoisoned`、`EventHelper.ChangePoisonByType` 和事件函数 `SpecifyPoisoned` 拦截太吾新增毒素，覆盖常规事件、脚本指定值和直接增量写入。其中 `SpecifyPoisoned` 单独处理事件函数先通过 `ref GetPoisoned()` 原地改值、再调用 `SetPoisoned` 的路径。

毒素查询不做同样的全局塑形。`Character.HasPoisonImmunity` 和 `GetPoisonResist` 被世界事件、物品选择和治疗逻辑复用；游戏中有路径先查找“太吾未免疫的毒素”，随后立即读取对应毒物配置。若把太吾全局伪装为所有毒素免疫，查找结果可能为空并导致后续配置访问失效。因此战斗外毒素采用“写入拦截”而不是“毒免查询伪装”：新增毒素不会落盘，已有毒素和原版毒抗显示不被主动改写。

### AI 可见性

当前版本不 patch 敌方 AI 评分函数。战斗 AI 能感知的数值表现来自游戏原本会读取的战斗角色免疫和毒抗；公共入口拦截和真实人物写入兜底不会额外改写 AI 评分。攻击侧选招中直接读取人物静态破体/破气抗性的路径保持原样，也不通过人为 damage step 哨兵影响评分。

## 游戏侧复用

- 免疫塑形复用 `CombatDomain.SetDefeatMarkImmunity`，不再直接写 `CombatCharacter` 的免疫字段。
- 毒抗塑形复用 `GlobalConfig.MaxPoisonResistance` 和 `PoisonType.Count`，不复制毒抗阈值或毒类型数量。
- 真实人物伤势回写的部位遍历使用 `BodyPartType.Count`，不硬编码身体部位数量。
- 太吾身份判断要求角色域中存在当前太吾角色，避免把 `TaiwuDomain` 初始化默认值当成有效太吾。
- 不通过反射读写游戏状态；后端项目 publicize `GameData` 后强类型引用游戏 API，Harmony patch 点用 `nameof(...)` 描述。
- `CombatCharacter.Init` 使用 Harmony transpiler 锚定唯一的 `StateMachine.Init` 调用后插入，避免初始状态切换先于战斗角色塑形发生，并在游戏时序漂移时失败暴露。
- `SetDefeatMarkImmunity` 和 `SetPoisonResist` 入口保留同一套塑形规则，防止战斗中的游戏同步或事件调用覆盖战斗角色对象。
- 致命伤入口不按功法逐一补丁。规则层维护一个仅供外伤满溢结算使用的临时作用域；`CombatDomain.AddInjuryDamageValue` 和 `CombatDomain.ApplyMixedInjury` 中的 `AddFatalDamage` 调用被替换到同一个包装方法，只有 `type == 0` 的外伤侧满溢会进入该作用域，内伤满溢和其它直接调用仍交由致命入口裁决。
- 特殊效果产生的直接致命效果同样不按具体功法补丁。运行时扫描 `SpecialEffectBase` 后代中会调用致命伤、致命标记或游戏现有致命伤中间入口的方法，并在这些方法执行期间记录效果持有人；太吾作为效果来源时跳过这类直接致命效果。漂移信号是游戏新增不经这些 `CombatCharacter` 入口写入致命伤或致命标记，或新增未纳入扫描的致命伤中间入口。
- 特殊效果产生的战斗状态变化同样通过来源作用域归因。运行时扫描 `SpecialEffectBase` 后代中会调用 `CombatDomain.AddCombatState` 任一重载的方法，并在这些方法执行期间记录效果持有人；太吾作为效果来源时跳过这类状态变化，即使原调用没有传 `srcCharId`。漂移信号是游戏新增不经 `AddCombatState` 写入战斗状态，或新增未纳入扫描的状态中间入口。
- 特殊效果来源扫描只把明确解析到目标入口的方法纳入作用域；扫描过程无法解析调用 token 时失败暴露，避免把未知调用静默当作未命中。
- 功法特殊效果注册集中 patch `SpecialEffectDomain.Add`；规则判定复用 `CombatSkillEffectActiveType`，只拦截 `Cast` 和 `EnterCombat`，`Equipped`、`Broken`、读档和跨存档特效注册仍由游戏原逻辑拥有。该 patch 只额外保持 `Add` 的原版类型解析边界：方法内必须存在唯一一次 `Type.GetType(string)` 特效类型解析，并被替换为从 `SpecialEffectDomain` 所在程序集解析；调用形态漂移时失败暴露。
- 功法特殊效果属性修正集中 patch `SpecialEffectDomain.GetModifyValue`、`GetTotalPercentModifyValue` 和 `CalcCustomModifyEffectList`。同一个规则只过滤 `CombatSkillEffectBase`：`Cast` 和 `EnterCombat` 特效只要目标或来源涉及战斗中的太吾就不参与本次修改计算；`Equipped` 和 `Broken` 特效在目标和来源同为战斗中的太吾时保留，用于表达装备或突破被动的自身数值，跨角色影响仍跳过。`GetModifyValue` 和 `GetTotalPercentModifyValue` 只在游戏原本枚举 `AffectDatas.TryGetValue` 前插入过滤，不复制或接管原版求和规则。漂移信号是游戏新增 `EDataModifyType`、新增不经过这些入口枚举 `SpecialEffectList` 的属性修正路径，或这些入口的特效枚举 IL 形态变化导致注入计数失败。
- 真实人物伤毒写入兜底优先 patch 游戏已有 setter/change 入口，不通过替换配置表或给太吾新增人物特性表达规则。
- 毒素写入遍历复用 `PoisonType.Count`，不复制毒素类型数量。
- 新增 `ref GetPoisoned()` 原地改毒路径是毒素兜底的漂移信号；这类路径不能只靠 `SetPoisoned` 恢复旧值，需要补对应入口。
- 对没有游戏语义常量支撑的 damage step “无限大”做法保持删除，避免把任意数值固化成规则。

## Publicizer

本 Mod 后端项目 publicize `GameData`，用于强类型引用游戏的非 public 成员。Harmony patch 私有游戏方法时使用 `nameof(...)`，避免字符串字面量定位游戏代码。

Publicizer 目标保持在 `GameData`，因为当前非 public patch 点都在后端 `GameData` 里。新增其它程序集的非 public API 访问时，再按实际需要增加更窄或新的 `Publicize` 项。

## 边界

- 战斗角色塑形只适用于战斗域。战斗外只兜底真实人物新增内伤、新增毒素和人物免疫查询；其它活动如果也有阶段性角色对象，需要先确认对应活动的生命周期和 AI/判定读取路径，不能直接套用 `CombatCharacter` 规则。
- 对外伤相关的基础流程不做免疫，不主动降低敌方外伤输出，也不阻断普通外伤特殊效果的基础伤害落点。外伤满溢产生的致命伤视为普通外伤流程的一部分，保留游戏原行为；太吾受到或造成的直接施加致命伤、致命标记和致命标记转移无效。
- 死亡标记和强制败北不属于本规则的阶段免疫或数值抬高范围；这类机制由游戏的死亡免疫、剧情或强制败北边界拥有。若新增游戏路径绕过公共伤害、致命标记和公共特殊效果入口，需要先定位可复用入口再扩展规则。
- 战斗状态豁免只阻止太吾作为目标时的新增或正向叠加状态，不主动移除进入战斗前或其它系统已经写入的状态。`RemoveCombatState` 不做拦截；`AddCombatState` 中对太吾目标的非正值变化也交回游戏处理，因为这类变化可能是降低、抵消或清除已有状态。太吾作为来源时，`AddCombatState` 造成的状态变化一律跳过。
- 真气、功法配置和身法本体提供的移动速度基础配置不是运行时功法特殊效果对象，仍按游戏原本的角色、真气、`AffectingMoveSkillId` 和功法配置计算；装备或突破被动功法特殊效果只在目标和来源同为太吾时保留自身修正。身法附带的功法特殊效果若通过 `CombatSkillEffectBase` 跨角色修改移动速度、攻速、施展速度或其它字段，则按上面的太吾相关过滤规则跳过。
- 特殊效果属性修正过滤只针对功法特殊效果对象。装备、职业、奇书或其它非 `CombatSkillEffectBase` 特效不会仅因本规则被统一跳过；它们若落到公共伤害、毒素、状态或致命伤入口，仍由对应公共入口裁决。直接写字段、事件回调内部改状态机或其它不经过公共入口的副作用，需要先确认语义再单独补入口。
- 既有内伤和毒素不主动清理。清伤、减伤、解毒和降毒仍按游戏原逻辑执行。
