# 角色经历读取指引

当玩家询问某个角色最近发生了什么、生平经历、过往事件、经历分类或经历摘要，且需要读取当前存档事实时，
读取本文件。角色经历属于后端权威状态；优先读取数据本身，再把数据整理为玩家可理解的答复。

本文件是角色经历的领域专门指引，负责说明 `LifeRecord` 的读取入口、返回数据和解释边界。脚本入口契约、
目标侧和线程规则仍归 `RUNTIME_SCRIPTING.md`；配置、本地化和通用模板资料归 `GAME_KNOWLEDGE.md`。

本文件随相枢 Mod 发布，面向订阅玩家的默认运行时；只依赖当前游戏进程中已加载的游戏程序集、配置表和相枢
脚本工具。若运行时类型、成员或工具不可用，按实际工具结果收窄问题。

## 主路径

默认处理顺序：

1. 确定目标角色 `charId`。未指定时先用当前太吾：`DomainManager.Taiwu.GetTaiwuCharId()`。
2. 在 `backend` 目标侧、`mainThread` 入口读取：`DomainManager.LifeRecord.GetReversedRecord(...)`。
3. 过滤 `RecordType < 0` 的前端辅助记录，保留真实经历记录。
4. 用 `Config.LifeRecord.Instance[recordType]` 解释经历类型、描述模板、参数类型、分类和好感要求。
5. 按可见性边界组织答复；参数未还原为玩家文本时，使用经历类型、日期、模板和已确认参数说明事实，
   不把未渲染模板当作最终游戏文案。

## 读取入口

后端核心域是 `GameData.Domains.LifeRecord`：

- `DomainManager.LifeRecord.GetReversedRecord(context, charId, startCount, readCount)`：读取角色经历列表。游戏角色菜单
  用这个入口分页读取整页经历；`startCount` 是已经读取的经历数量，`readCount` 是本次读取数量。返回
  `TransferableLifeRecordData`。
- `DomainManager.LifeRecord.GetReversedRecord(context, charId, startCount, readCount, isDreamBack)`：同上，但
  `isDreamBack = true` 时读取梦回经历。

调用脚本时选 `targetSide = "backend"`、`entryThread = "mainThread"`。只需要当前太吾经历时不传 `charId`；
查询指定角色时从当前上下文、玩家可见 UI 或其它角色查询取得 `charId` 后再传入。

## 最小读取脚本

动态脚本在后端主线程上可以用 `DataContextManager.GetCurrentThreadDataContext()` 取得当前线程 `DataContext`：

```csharp
using System.Linq;
using System.Threading.Tasks;
using Config;
using GameData.Common;
using GameData.Domains;
using Wanxiang.Xiangshu.Scripting;

public static class XiangshuScript
{
    public static Task<object?> ExecuteAsync(XiangshuScriptGlobals globals)
    {
        int charId = DomainManager.Taiwu.GetTaiwuCharId();
        if (globals.Arguments.TryGetValue("charId", out string rawCharId)
            && int.TryParse(rawCharId, out int parsedCharId))
        {
            charId = parsedCharId;
        }

        int count = 20;
        if (globals.Arguments.TryGetValue("count", out string rawCount)
            && int.TryParse(rawCount, out int parsedCount))
        {
            count = parsedCount;
        }

        var context = DataContextManager.GetCurrentThreadDataContext();
        var data = DomainManager.LifeRecord.GetReversedRecord(context, charId, 0, count);
        var records = data.Record
            .Where(record => record.RecordType >= 0)
            .Select(record =>
            {
                var config = Config.LifeRecord.Instance[record.RecordType];
                return new
                {
                    date = new
                    {
                        raw = record.Date,
                        year = record.Date / 12 + 1,
                        month = record.Date % 12 + 1,
                    },
                    recordType = record.RecordType,
                    name = config?.Name,
                    descTemplate = config?.Desc,
                    displayType = config?.DisplayType.ToString(),
                    requiredFavorability = config?.RequiredFavorability,
                    arguments = record.Arguments
                        .Select(arg => new { type = arg.Item1, index = arg.Item2 })
                        .ToArray(),
                };
            })
            .ToArray();

        return Task.FromResult<object?>(new
        {
            charId = data.CharId,
            taiwuCharId = data.TaiwuCharId,
            favorToTaiwu = data.FavorToTaiwu,
            lifeRecordCount = data.LifeRecordCount,
            records,
        });
    }
}
```

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

本文件只覆盖角色经历的读取和解释，不提供创建、修改或提交经历的流程。玩家要求改变经历时，按
`RUNTIME_SCRIPTING.md` 的写操作规则处理：先明确目标和副作用，不清楚时保持只读。
