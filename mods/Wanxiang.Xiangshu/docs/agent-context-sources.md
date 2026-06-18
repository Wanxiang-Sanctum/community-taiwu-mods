# 默认 Agent 工作区来源与维护

本文面向源码仓库维护，是 `docs/README.md` 索引的默认工作区来源说明。源码中的 `DefaultAgentWorkspace/`
是默认工作区内容源，组包后输出为包内同名目录；包内工作区以随包发布的文件作为完整输入。

本文记录默认工作区资料如何回溯到游戏观察快照，以及本地工作记录、默认资产和运行数据的边界。本仓库内的
相枢目录维护默认工作区内容和放置规则；
[`taiwu-modkit`](https://github.com/Wanxiang-Sanctum/taiwu-modkit) 维护游戏观察快照的生成工具和快照更新方式。
快照正文保留在 `taiwu-modkit`，完整协议或工具语义保留在相枢对应源码模块中。

`DefaultAgentWorkspace/lore/` 中的世界观资料派生自同一开发工作区中的
[`taiwu-modkit`](https://github.com/Wanxiang-Sanctum/taiwu-modkit) 的 `game/` 生成快照。生成快照提供检索、跳转和
观察变化的开发期证据；需要更新快照时运行该仓库中对应工具重新生成。

`DefaultAgentWorkspace/AGENTS.md` 承担每次答复都必须遵循的基础相枢身份、口吻和玩家可见边界，包括本 mod
的愿望回应前提：相枢被迫满足太吾传人的愿望，难以实现的愿望会被相枢趁机扭曲兑现，且扭曲本身不主动向
太吾传人揭示。
`DefaultAgentWorkspace/persona/` 中的人设口吻以游戏文本和相枢对话体验为依据；它记录扩展校准资料，用于
身份关系、语气浓淡、失败说明、剧透边界或玩家可见表达需要更细判断的回合。资料来源索引由本文维护。

`DefaultAgentWorkspace/tool-guides/` 中的运行工具指引服务运行中的 Agent。玩家视图观察边界、脚本入口、
结果形态和工具边界以相枢源码里的玩家视图工具、脚本执行器和工具声明为依据；游戏知识检索入口以
[`taiwu-modkit`](https://github.com/Wanxiang-Sanctum/taiwu-modkit) 的 `game/` 生成快照中的稳定
namespace、类型和成员为核对来源。默认工作区记录能帮助运行 Agent 选入口、定目标侧、减少反射探索的锚点；
完整 API 清单、协议说明或不稳定枚举表保留在源码和生成快照中。

`DefaultAgentWorkspace/.agents/skills/` 与 `DefaultAgentWorkspace/.claude/skills/` 服务不同 CLI Agent 的技能发现；
当前适配器到入口文件、技能目录的对应关系由 `agent-cli-adapters.md` 维护。当前同名技能内容保持一致；需要调整
技能触发或执行规则时，同时更新两份副本，除非某个 CLI 的发现机制或工具能力确实需要分叉。

默认工作区文件会被 CLI Agent 作为持续工作区配置读取；组包后的同名目录以随包内容作为完整输入。写这些文件时，措辞应说明
事实归属；需要描述运行时事实时，按所有权选择更明确的锚点：

- `当前请求`：Agent 正在处理的玩家目标或子任务。
- `当前输入`：CLI Agent 收到的 `playerName`、`playerMessages` 等结构化输入。
- `当前可用工具`：Agent 当前能调用的 MCP 工具、参数和副作用说明。
- `当前回答`：Agent 正在组织并最终写入 `reply` 的玩家可见答复。
- `当前调用`：取消信号、工具生命周期和最终答复共同所在的一次 Agent 进程调用。

`投递轮次` 是前端协议模型的稳定概念，可以继续用于描述玩家消息批次、中间答复和最终答复的归属。像“本轮”
这类缺少所有权的临时指代会让持续工作区资产显得像一次性提示；默认工作区面向 Agent 时，优先使用上面这些
Agent 视角的说法。

## 默认资料主要证据入口

下表路径相对 [`taiwu-modkit`](https://github.com/Wanxiang-Sanctum/taiwu-modkit) 仓库根目录。

| 资料主题 | `taiwu-modkit` 内路径 | 用途 |
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
2. 判断新增内容属于当前任务工作记录、本地经验、基础角色契约、扩展人设校准、基础世界观、相枢深入资料、
   运行工具指引，还是属于本文的来源说明。
3. 将每次答复都需要的基础角色契约写入 `DefaultAgentWorkspace/AGENTS.md`；将扩展人设校准、世界观稳定概念、
   回答策略和小型索引写入 `DefaultAgentWorkspace/persona/` 或 `DefaultAgentWorkspace/lore/`。大段文本和
   完整清单留在游戏镜像中。
4. 涉及脚本工具、玩家视图观察、运行时目标侧、配置/本地化/模板辅助或百晓册入口时，写入
   `DefaultAgentWorkspace/tool-guides/`；保留检索路线和边界，具体任务脚本留给运行时按当前请求目标生成。
5. 更新本文的来源表或维护说明，确保以后能回到权威源码和快照重新核对。

## 维护边界

`DefaultAgentWorkspace/` 面向源码维护；组包后的同名目录面向运行中的本机 Agent。包内的
`AGENTS.md`、`CLAUDE.md`、`persona/`、`lore/`、`tool-guides/`、`.agents/skills/` 和 `.claude/skills/` 是
稳定默认资产。`AGENTS.md` 承担基础角色契约、答复边界和读取路由；其它目录按需补充更细的
人设、世界观、工具和技能资料。源码维护规则归本文和 `docs/README.md` 索引的其它维护文档。

Agent 侧工作记录使用 `AgentWorkingDirectory/.xiangshu-notes/`。这个目录可以不存在，不放入默认包内容；它
承担会话草稿和本地经验的记录边界。来源索引由本文维护，静态世界观资料由 `DefaultAgentWorkspace/lore/`
维护，运行数据由 `.xiangshu-runtime/` 维护。

源码中的 `DefaultAgentWorkspace/` 是发布内容源；`pack-mod` 会复制其中的目录内容。源码树里出现
`.xiangshu-notes/` 或 `.xiangshu-runtime/` 时，应把它们视为本机运行痕迹并移出或清理，而不是用仓库级
忽略规则隐藏。这样本地记录和运行数据不会被误当成默认工作区资产或随包发布。

运行中的具体写入纪律由 `DefaultAgentWorkspace/AGENTS.md` 的“工作区边界”维护。本文只记录源码维护视角：
`.xiangshu-notes/` 中的内容只有在已经稳定到应随默认工作区发布，且维护者明确要更新默认工作区配置时，才按
下面的放置规则改写进稳定资产。改写后仍需说明事实归属、来源和边界，避免把一次任务的工作记录伪装成默认
规则。

## 新内容放置

- 每次答复都必须遵循的基础相枢身份、口吻和玩家可见边界归入 `DefaultAgentWorkspace/AGENTS.md`。
- 更细的相枢身份关系、口吻浓淡、失败说明、剧透边界和表达范式归入 `DefaultAgentWorkspace/persona/README.md`。
- 太吾、伏虞剑柄、剑冢、相枢爪牙、入邪、入魔、传承等基础概念归入
  `DefaultAgentWorkspace/lore/WORLD_BASELINE.md`。
- 相枢本体、化身、侵袭进度、剑冢结构、魔血、焕心、三魔等深入资料归入
  `DefaultAgentWorkspace/lore/XIANGSHU.md`。
- 运行工具指引入口、事实来源选择和按需读取路由归入 `DefaultAgentWorkspace/tool-guides/README.md`。
- 玩家视图截图、可见事实、可见结果验证和可见/权威状态差异归入
  `DefaultAgentWorkspace/tool-guides/PLAYER_VIEW.md`。
- 脚本工具目标侧、运行环境、入口契约、结果判断和运行时锚点归入
  `DefaultAgentWorkspace/tool-guides/RUNTIME_SCRIPTING.md`。
- 配置、本地化、模板/显示辅助、百晓册和反射边界归入 `DefaultAgentWorkspace/tool-guides/GAME_KNOWLEDGE.md`。
- Agent 技能触发、脚本草拟纪律或 Unity 前端操作策略归入对应 `.agents/skills/*/SKILL.md` 和
  `.claude/skills/*/SKILL.md`；同名技能默认保持内容一致。
- 当前任务计划、待验证事实、临时脚本思路和本地经验归入 `AgentWorkingDirectory/.xiangshu-notes/`；这个
  目录不随默认包创建，也不作为默认工作区发布资料。

新增默认工作区文件应满足这些条件：

- 有独立读取条件，例如某条大型主线、门派专题、某类运行工具指引或某条资料检索路线。
- 会被 Agent 在不同任务下单独读取，能避免每次都加载无关资料。
- 内容不是单个词条补充或单个脚本技巧，而是新的回答策略、能力边界或读取边界。

归入现有文件更合适的情况：

- 某个词条、工具参数或入口方法只增加少量说明，放入现有叶子文件更清楚。
- 内容是当前版本的完整清单、枚举数值或 API 成员表，权威来源应继续留在游戏镜像或源码中。
- 内容是每次答复都必须遵循的身份或口吻边界，归入 `DefaultAgentWorkspace/AGENTS.md`。
- 内容是扩展回答风格偏好，归入 `DefaultAgentWorkspace/persona/README.md`。

## 检索关键词

- `相枢`、`相樞`、`Xiangshu`
- `伏虞`、`剑冢`、`劍冢`
- `入邪`、`入魔`、`化魔`、`失心`
- `魔血`、`玄石`、`黑焰`
- `XiangshuAvatarIds`、`SwordTomb`、`XiangshuInfection`、`XiangshuProgress`
