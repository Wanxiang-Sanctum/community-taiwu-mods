# 默认 Agent 上下文来源与维护

本文只服务源码仓库维护。包内 `AgentWorkspace/` 是自包含的默认模板和示范，不应要求运行中的 Agent 访问
`taiwu-modkit/game`、源码仓库或开发机绝对路径。

`AgentWorkspace/context/` 中的世界观资料派生自太吾工作区内 `taiwu-modkit/game` 下的生成快照。该目录由
工具生成，只用于检索、跳转和观察变化；需要更新快照时运行 `taiwu-modkit` 中对应工具，不手工修改镜像。

## 主要证据入口

| 资料主题 | 镜像路径 | 用途 |
| --- | --- | --- |
| 侵袭阶段、相枢入邪/魔通、剑冢异动提示 | `game/text/The Scroll Of Taiwu_Data/StreamingAssets/Language_CN/WorldState_language.txt.yml` | `WORLD_BASELINE.md` 和 `XIANGSHU_LORE.md` 的世界状态、入邪警告、破冢说明。 |
| 相枢入邪、化魔、化身、真身、焕心、三魔、似魔似人 | `game/text/The Scroll Of Taiwu_Data/StreamingAssets/Language_CN/CharacterFeature_language.txt.yml` | 相枢人设和深入语境素材。 |
| 剑冢名称与环境描述 | `game/text/The Scroll Of Taiwu_Data/StreamingAssets/Language_CN/MapBlock_language.txt.yml` | 剑冢表格和意象。 |
| 化身姓名 | `game/text/The Scroll Of Taiwu_Data/StreamingAssets/Language_CN/Character_language.txt.yml` | 少年/常驻化身名和化身称号。 |
| 魔血、玄石、伏虞剑柄黑焰 | `game/text/Event/EventLanguages/Taiwu_EventPackage_Adventure_MainStory_XiangshuBlood_Language_CN.txt.yml` | 魔血意象和相枢口吻素材。 |
| 相枢化身索引、模板范围、剑冢地图块 ID | `game/src/Backend/GameData.Shared/GameData.Domains.World/XiangshuAvatarIds.cs` | 化身和剑冢配置关系。 |
| 剑冢配置项、AdventureCoreId、化身模板 | `game/src/Backend/GameData.Shared/Config/SwordTomb.cs` | 剑冢索引和化身配置。 |
| 相枢等级与世界状态映射 | `game/src/Backend/GameData.Shared/GameData.Domains.World/SharedMethods.cs` | `xiangshuProgress / 2` 与世界状态选择。 |
| 剑冢状态枚举 | `game/src/Backend/GameData.Shared/GameData.Domains.World/SwordTombStatus.cs` | 未完成、第一阶段击败、第二阶段击败。 |
| 神剑碎片增加相枢侵蚀、剑冢倒计时、侵蚀上限 | `game/src/Backend/GameData.Shared/GlobalConfig.cs` | 机制说明中的数值依据。 |

## 更新流程

1. 先在 `taiwu-modkit/game/text` 和 `taiwu-modkit/game/src` 中检索相关关键词。
2. 判断新增资料属于基础语境、相枢深入语境、人设口吻，还是只应记录在本文。
3. 只把稳定概念、回答策略和小型索引写入 `AgentWorkspace/context/`。大段文本和完整清单留在游戏镜像中。
4. 更新本文的来源表，确保以后能回到权威快照重新核对。

## 维护边界

`AgentWorkspace/` 面向运行中的本机 Agent。包内的 `AGENTS.md`、`CLAUDE.md`、`context/`、`.agents/skills/`
和 `.claude/skills/` 是稳定模板资产；它们告诉 Agent 如何回答、如何读取上下文、哪些运行目录不能碰。不要在
这些文件里放“如何维护本目录”“来源路径在哪里”这类源码维护规则。

源码维护规则写在 `docs/` 下。用户仍可把 `AgentWorkingDirectory` 指向自己的目录或手工改默认工作区；包内
文档只表达运行 Agent 需要遵循的工作区边界。

Agent 侧临时记录使用 `AgentWorkspace/.xiangshu-notes/`。这个目录可以不存在，不放入默认包内容；它只承担
工作记录边界，不承担来源索引、静态语境或运行数据所有权。

## 新内容放置

- 相枢身份、口吻、失败说明和玩家可见边界归入 `AgentWorkspace/context/PERSONA.md`。
- 太吾、伏虞剑柄、剑冢、相枢爪牙、入邪、入魔、传承等基础概念归入
  `AgentWorkspace/context/WORLD_BASELINE.md`。
- 相枢本体、化身、侵袭进度、剑冢结构、魔血、焕心、三魔等深入资料归入
  `AgentWorkspace/context/XIANGSHU_LORE.md`。

新增上下文文件应满足这些条件：

- 有独立读取条件，例如某条大型主线、门派专题或相枢化身专门资料。
- 会被 Agent 在不同任务下单独读取，能避免每次都加载无关资料。
- 内容不是单个词条补充，而是新的回答策略或新的读取边界。

不适合新增文件的情况：

- 只是某个词条多一两句话，放入现有叶子文件更清楚。
- 只是当前版本的完整清单，未来容易变成陈旧副本。
- 只是回答风格偏好，应该归入 `PERSONA.md`。

## 检索关键词

- `相枢`、`相樞`、`Xiangshu`
- `伏虞`、`剑冢`、`劍冢`
- `入邪`、`入魔`、`化魔`、`失心`
- `魔血`、`玄石`、`黑焰`
- `XiangshuAvatarIds`、`SwordTomb`、`XiangshuInfection`、`XiangshuProgress`
