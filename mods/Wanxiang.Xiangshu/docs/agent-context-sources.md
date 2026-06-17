# 默认 Agent 工作区来源与维护

本文只服务源码仓库维护。源码中的 `DefaultAgentWorkspace/` 是默认工作区内容源，组包后输出为包内同名目录。
包内工作区必须自包含，不应要求运行中的 Agent 访问 `taiwu-modkit/game`、源码仓库或开发机绝对路径。

`DefaultAgentWorkspace/lore/` 中的世界观资料派生自太吾工作区内 `taiwu-modkit/game` 下的生成快照。生成快照
只用于检索、跳转和观察变化；需要更新快照时运行 `taiwu-modkit` 中对应工具，不手工修改镜像。

`DefaultAgentWorkspace/persona/` 中的人设口吻以游戏文本和相枢对话体验为依据；它只记录玩家可见身份、语气
和失败说明，不承载资料来源索引。

`DefaultAgentWorkspace/tool-guides/` 中的运行工具指引不是玩家可见资料库。脚本入口、结果形态和工具边界以
相枢源码里的脚本执行器和工具声明为依据；游戏知识检索入口以 `taiwu-modkit/game` 生成快照中的稳定
namespace、类型和成员为核对来源。写入默认工作区时只记录能帮助运行 Agent 选入口、定目标侧、减少反射探索的
锚点，不复制完整 API 清单、协议说明或不稳定枚举表。

## 默认资料主要证据入口

| 资料主题 | 镜像路径 | 用途 |
| --- | --- | --- |
| 侵袭阶段、相枢入邪/魔通、剑冢异动提示 | `game/text/The Scroll Of Taiwu_Data/StreamingAssets/Language_CN/WorldState_language.txt.yml` | `WORLD_BASELINE.md` 和 `XIANGSHU.md` 的世界状态、入邪警告、破冢说明。 |
| 相枢入邪、化魔、化身、真身、焕心、三魔、似魔似人 | `game/text/The Scroll Of Taiwu_Data/StreamingAssets/Language_CN/CharacterFeature_language.txt.yml` | 相枢人设和深入资料素材。 |
| 剑冢名称与环境描述 | `game/text/The Scroll Of Taiwu_Data/StreamingAssets/Language_CN/MapBlock_language.txt.yml` | 剑冢表格和意象。 |
| 化身姓名 | `game/text/The Scroll Of Taiwu_Data/StreamingAssets/Language_CN/Character_language.txt.yml` | 少年/常驻化身名和化身称号。 |
| 魔血、玄石、伏虞剑柄黑焰 | `game/text/Event/EventLanguages/Taiwu_EventPackage_Adventure_MainStory_XiangshuBlood_Language_CN.txt.yml` | 魔血意象和相枢口吻素材。 |
| 相枢化身索引、模板范围、剑冢地图块 ID | `game/src/Backend/GameData.Shared/GameData.Domains.World/XiangshuAvatarIds.cs` | 化身和剑冢配置关系。 |
| 剑冢配置项、AdventureCoreId、化身模板 | `game/src/Backend/GameData.Shared/Config/SwordTomb.cs` | 剑冢索引和化身配置。 |
| 相枢等级与世界状态映射 | `game/src/Backend/GameData.Shared/GameData.Domains.World/SharedMethods.cs` | `xiangshuProgress / 2` 与世界状态选择。 |
| 剑冢状态枚举 | `game/src/Backend/GameData.Shared/GameData.Domains.World/SwordTombStatus.cs` | 未完成、第一阶段击败、第二阶段击败。 |
| 神剑碎片增加相枢侵蚀、剑冢倒计时、侵蚀上限 | `game/src/Backend/GameData.Shared/GlobalConfig.cs` | 机制说明中的数值依据。 |

## 更新流程

1. 静态世界观先在 `taiwu-modkit/game/text` 和 `taiwu-modkit/game/src` 中检索相关关键词；运行工具指引先核对
   相枢源码中的脚本执行器和工具声明，再按需要查游戏生成快照。
2. 判断新增资料属于人设口吻、基础世界观、相枢深入资料、运行工具指引，还是只应记录在本文。
3. 只把稳定概念、回答策略和小型索引写入 `DefaultAgentWorkspace/persona/` 或
   `DefaultAgentWorkspace/lore/`。大段文本和完整清单留在游戏镜像中。
4. 涉及脚本工具、运行时目标侧、配置/本地化/模板辅助或百晓册入口时，写入
   `DefaultAgentWorkspace/tool-guides/`；只保留检索路线和边界，不写具体任务脚本。
5. 更新本文的来源表或维护说明，确保以后能回到权威源码和快照重新核对。

## 维护边界

`DefaultAgentWorkspace/` 面向源码维护；组包后的同名目录面向运行中的本机 Agent。包内的
`AGENTS.md`、`CLAUDE.md`、`persona/`、`lore/`、`tool-guides/`、`.agents/skills/` 和 `.claude/skills/` 是
稳定默认资产；它们告诉 Agent 如何回答、如何读取上下文、哪些运行目录不能碰。不要在这些文件里放“如何维护
本目录”“来源路径在哪里”这类源码维护规则。

源码维护规则写在 `docs/` 下。用户仍可把 `AgentWorkingDirectory` 指向自己的目录或手工改默认工作区；工作区内
文档只表达运行 Agent 需要遵循的工作区边界。

Agent 侧临时记录使用 `AgentWorkingDirectory/.xiangshu-notes/`。这个目录可以不存在，不放入默认包内容；它
只承担工作记录边界，不承担来源索引、静态世界观资料或运行数据所有权。

## 新内容放置

- 相枢身份、口吻、失败说明和玩家可见边界归入 `DefaultAgentWorkspace/persona/README.md`。
- 太吾、伏虞剑柄、剑冢、相枢爪牙、入邪、入魔、传承等基础概念归入
  `DefaultAgentWorkspace/lore/WORLD_BASELINE.md`。
- 相枢本体、化身、侵袭进度、剑冢结构、魔血、焕心、三魔等深入资料归入
  `DefaultAgentWorkspace/lore/XIANGSHU.md`。
- 运行工具指引入口和按需读取路由归入 `DefaultAgentWorkspace/tool-guides/README.md`。
- 脚本工具目标侧、运行环境、入口契约、结果判断和运行时锚点归入
  `DefaultAgentWorkspace/tool-guides/RUNTIME_SCRIPTING.md`。
- 配置、本地化、模板/显示辅助、百晓册和反射边界归入 `DefaultAgentWorkspace/tool-guides/GAME_KNOWLEDGE.md`。

新增默认工作区文件应满足这些条件：

- 有独立读取条件，例如某条大型主线、门派专题、某类运行工具指引或某条资料检索路线。
- 会被 Agent 在不同任务下单独读取，能避免每次都加载无关资料。
- 内容不是单个词条补充或单个脚本技巧，而是新的回答策略、能力边界或读取边界。

不适合新增文件的情况：

- 只是某个词条、工具参数或入口方法多一两句话，放入现有叶子文件更清楚。
- 只是当前版本的完整清单、枚举数值或 API 成员表，未来容易变成陈旧副本。
- 只是回答风格偏好，应该归入 `DefaultAgentWorkspace/persona/README.md`。

## 检索关键词

- `相枢`、`相樞`、`Xiangshu`
- `伏虞`、`剑冢`、`劍冢`
- `入邪`、`入魔`、`化魔`、`失心`
- `魔血`、`玄石`、`黑焰`
- `XiangshuAvatarIds`、`SwordTomb`、`XiangshuInfection`、`XiangshuProgress`
