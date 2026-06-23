# 角色经历检索指引

当玩家询问某个角色最近发生了什么、生平经历、过往事件、经历分类、经历摘要，或想查找符合某类经历的
角色时，读取本文件。角色经历属于后端权威状态；优先检索数据本身，再把数据整理为玩家可理解的答复。

本文件是角色经历的领域专门指引，负责说明检索模型、基础接口、可组合原语、返回数据和解释边界。脚本入口
契约、目标侧和线程规则仍归 `RUNTIME_SCRIPTING.md`；配置、本地化和通用模板资料归 `GAME_KNOWLEDGE.md`。

本文件随相枢 Mod 发布，面向订阅玩家的默认运行时；只依赖当前游戏进程中已加载的游戏程序集、配置表和相枢
脚本工具。运行时类型、成员和工具能力以实际工具结果为准。

## 查询模型

先判断玩家请求属于哪种检索方向：

- 按角色读经历：已知目标角色，读取这个角色的最近经历、生平经历、经历分类或经历摘要。
- 按经历找角色：已知经历条件，查找候选角色中谁拥有符合条件的经历。

两种方向共用同一套记录解释规则：过滤 `RecordType < 0` 的前端辅助记录，用
`Config.LifeRecord.Instance[recordType]` 解释经历类型、描述模板、参数类型、分类和好感要求，再按可见性边界
组织答复。参数未还原为玩家文本时，使用经历类型、日期、模板和已确认参数说明事实，
不把未渲染模板当作最终游戏文案。

## 按角色读经历

默认处理顺序：

1. 确定目标角色 `charId`。未指定时先用当前太吾：`DomainManager.Taiwu.GetTaiwuCharId()`。
2. 在 `backend` 目标侧、`mainThread` 入口读取：`DomainManager.LifeRecord.GetReversedRecord(...)`。
3. 根据玩家要求决定读取页数和摘要范围；普通最近经历先读小页，完整生平再分页继续。
4. 按查询模型解释记录并组织答复。

## 按经历找角色

这类查询的主语不是单个角色，而是“候选角色集合 + 经历谓词”。主路径是先收窄候选角色集合，再按角色分页读取
经历并匹配谓词。

默认处理顺序：

1. 解析经历谓词。能确定模板时优先匹配 `recordType`；不能确定时先用 `Config.LifeRecord.Instance` 按
   `Name`、`Desc`、`DisplayType`、`Parameters` 找候选模板。
2. 确定候选角色集合。优先使用玩家指定范围、当前界面可见角色、当前太吾相关人物、指定地区/村镇/门派/囚犯等
   已有角色集合；范围不清时先收窄候选范围。
3. 对每个候选 `charId` 调用 `GetReversedRecord(context, charId, startCount, readCount)` 分页读取，过滤辅助记录后
   按经历谓词匹配。
4. 找到足够结果后停止；需要完整结果时继续分页到角色经历读完，再处理下一个候选。
5. 返回角色、匹配到的经历、日期、模板名和已确认参数；结果很多时先按时间近、匹配度高或玩家指定范围排序并限量。

候选范围优先级：

- 已有 `charId` 列表、当前太吾、当前界面选中的角色或玩家明确点名的角色。
- 玩家指定的人际范围、地点范围、组织范围、村镇范围、囚犯范围或死者范围。
- 全部活人是广域兜底候选源；仅在玩家明确要求广域调试，或没有更窄候选源时使用，并说明范围大、耗时和结果
  可能需要分页。

按经历找角色不依赖前端渲染文本做匹配。优先匹配结构化字段：

- `recordType`：最稳定，适合“有某类经历的人”。
- `DisplayType`：适合“战斗类经历”“罪行类经历”等分类查询。
- `Date`：适合“最近一年”“某月前后”等时间范围。
- `Arguments`：适合“经历里涉及某人/地点/物品”的查询；先按参数类型和值匹配，命中后再渲染名称。

当前可用接口不提供按经历条件直接查找角色的稳定索引；这类查询由候选扫描承担。候选范围、每人读取页数和
返回数量都应服务玩家目标；范围过大且玩家没有明确要求完整扫描时，返回可解释的前若干命中和剩余范围。

## 基础接口

后端核心域是 `GameData.Domains.LifeRecord`：

- `DomainManager.LifeRecord.GetReversedRecord(context, charId, startCount, readCount)`：读取角色经历列表。游戏角色菜单
  用这个入口分页读取整页经历；`startCount` 是已经读取的经历数量，`readCount` 是本次读取数量。返回
  `TransferableLifeRecordData`。
- `DomainManager.LifeRecord.GetReversedRecord(context, charId, startCount, readCount, isDreamBack)`：同上，但
  `isDreamBack = true` 时读取梦回经历。
- `DomainManager.LifeRecord.GetRelated(...)` 虽在方法表中出现，但当前实现不支持；按经历找角色使用候选角色扫描。

调用脚本时选 `targetSide = "backend"`、`entryThread = "mainThread"`。只需要当前太吾经历时不传 `charId`；
查询指定角色时从当前上下文、玩家可见 UI 或其它角色查询取得 `charId` 后再传入。按经历找角色复用这个读取入口，
区别只在于外层先组织候选角色集合，再逐个读取并匹配。

## 可组合运行时原语

本节只提供可组合原语。完整入口类、`using`、返回值和工具参数处理按 `RUNTIME_SCRIPTING.md` 的脚本入口契约
临时组织。

读取某个角色的一页经历：

```csharp
var context = DataContextManager.GetCurrentThreadDataContext();
var data = DomainManager.LifeRecord.GetReversedRecord(context, charId, startCount, readCount);
var records = data.Record.Where(record => record.RecordType >= 0);
```

解释记录模板：

```csharp
var item = Config.LifeRecord.Instance[record.RecordType];
var displayType = item?.DisplayType;
var requiredFavorability = item?.RequiredFavorability;
```

按结构化字段匹配经历：

```csharp
bool matched = candidateRecordTypes.Contains(record.RecordType)
    && record.Date >= startDate
    && record.Date <= endDate;
```

常用候选角色来源：

```csharp
int taiwuCharId = DomainManager.Taiwu.GetTaiwuCharId();

var related = new HashSet<int>();
DomainManager.Character.GetAllRelatedCharIds(taiwuCharId, related);

var deadRelated = new HashSet<int>();
DomainManager.Character.GetAllRelatedDeadCharIds(taiwuCharId, deadRelated, includeGeneral: true);

var settlementChars = new List<GameData.Domains.Character.Character>();
DomainManager.Organization.GetCharactersFromSettlement(settlementId, minGrade, maxGrade, settlementChars);

var prisoners = new List<int>();
DomainManager.Organization.GetAllPrisoner(prisoners);
```

全量活人候选源：

```csharp
var allAliveNames = DomainManager.Character.GmCmd_GetAllCharacterName();
```

`GmCmd_GetAllCharacterName()` 只读返回当前活人候选；因范围大，普通查询优先使用更窄的玩家目标、可见角色、
关系网、村镇、组织或囚犯范围。

## 数据解释

`TransferableLifeRecordData` 继承 `TransferableRecordDataBase`，常用字段：

- `Record`：`TransferableRecord` 列表。用于读取时从最近经历开始分页；前端列表显示会再按 UI 需要索引。
- `LifeRecordCount`：真实经历数量，不包含日期、出生和分隔线等额外记录。
- `HeaderCount`、`ExtraCount`：前端显示用的额外记录数量；普通摘要通常不需要。
- `CharId`、`TaiwuCharId`、`FavorToTaiwu`：目标角色、当前太吾、目标角色对太吾好感。
- `StartDate`、`EndDate`：本批数据覆盖的日期范围。日期单位是月序号，`year = date / 12 + 1`，
  `month = date % 12 + 1`。
- `CharNames`、`LocationNames`、`SettlementNames`、`JiaoLoongNames`：后端已经补齐的部分名称数据。
- `ArgumentCollection`：参数池，保存地点、物品、文本、浮点数等不适合直接塞进 `TransferableRecord.Arguments`
  的参数。

`TransferableRecord` 常用字段：

- `Date`：事件发生月序号。
- `RecordType`：经历模板 id。负数是前端辅助记录：`-3` 分隔线，`-2` 日期标题，`-1` 出生记录；做经历索引或摘要时
  通常过滤掉负数。
- `Arguments`：`(paramType, indexOrValue)` 列表。`paramType` 对应
  `GameData.Domains.LifeRecord.GeneralRecord.ParameterType`；参数模板来自
  `Config.LifeRecord.Instance[recordType].Parameters`。

`Config.LifeRecordItem` 给经历模板元数据：

- `Name`：经历类型名。
- `Desc`：可格式化描述模板。
- `Parameters`：参数类型名数组，例如 `Character`、`Location`、`Item`、`Integer`。
- `RequiredFavorability`：前端显示此经历需要的好感阈值；当前太吾自己的经历通常可见。
- `ScoreType`、`Score`：经历分数/年度摘要使用。
- `DisplayType`：经历分类，枚举顺序是 `Great`、`Normal`、`Relation`、`Study`、`Produce`、`Combat`、`Negative`、
  `Crime`。

## 可见性与显示

非太吾角色的经历存在好感可见性：前端在 `FavorToTaiwu < RequiredFavorability` 时显示“好感不足，难以得知”
一类占位内容。普通答复应尊重这个玩家可见边界；除非玩家明确要求调试或读取隐藏状态，不要把隐藏经历伪装成
玩家自然可知的信息。

角色死亡后，`GetReversedRecord` 会在活跃角色不存在时回退读取游戏生成的死者经历快照。
这不是完整活人经历表；死者快照由游戏在死亡相关流程中生成。

普通问答不需要复刻前端富文本。优先返回结构化事实：日期、`RecordType`、`LifeRecordItem.Name`、
`LifeRecordItem.Desc`、参数类型和已确认名称。

需要核对游戏如何显示时，优先取得玩家视图。确实需要读取运行时前端显示逻辑时，再参考：

- `GameMessageUtils.ReadArguments(paramType, index, data)`：把参数还原成人名、地点、物品、功法等显示文本。
- `Game.Components.Character.LifeRecord.RenderedRecordData.SetData(...)`：把 `TransferableRecord` 转成前端 `Main`
  文本，并执行好感不足占位逻辑。

## 写入边界

本文件只覆盖角色经历的检索和解释，不提供创建、修改或提交经历的流程。玩家要求改变经历时，按
`RUNTIME_SCRIPTING.md` 的写操作规则处理：先明确目标和副作用，不清楚时保持只读。
