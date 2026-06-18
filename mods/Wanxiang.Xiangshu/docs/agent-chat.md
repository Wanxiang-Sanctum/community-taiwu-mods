# 相枢对话与本机 Agent 内部设计

## 目标形态

相枢的对话入口由前端插件负责。玩家界面呈现为与 NPC “相枢”的连续对话；前端负责游戏内消息、投递状态
和 CLI 调用，本机 Agent 自己维护长期会话上下文。交互以流畅度优先：前端启动 CLI Agent 时默认使用对应
适配器的完全信任式非交互执行方式，让一次游戏内投递轮次收束为一条相枢最终回应。具体 CLI 清单、命令形态、
工作区入口、会话 id 来源和最终答复来源见 `agent-cli-adapters.md`。

游戏内投递协议归前端会话所有；长期上下文归本机 Agent 会话所有。MCP server 的职责是作为相枢暴露给
本机 Agent 的工具服务：前端在启动 CLI Agent 时，把当前相枢 MCP endpoint 注册给 Agent；Agent 需要
读取或操作相枢能力时，再通过 MCP 调用相枢工具。

MCP server 可以额外提供 Agent 中间答复工具。Agent 调用这个工具时，MCP server 在内部处理后通过 IPC
通知前端追加一条相枢消息。中间答复是同一投递轮次最终答复之前的可见回应入口；最终答复仍由 CLI
调用结果收束。

MCP server 使用本机 Streamable HTTP stateless endpoint。它不维护 MCP 协议会话状态，`MCP-Session-Id`
不参与请求归属判断；相枢的聊天状态归前端投递会话，长期上下文归本机 Agent 会话。

前端组合根在游戏启动时生成本次运行的 MCP bearer token，并把它分别注入 MCP sidecar 进程和后续 CLI
Agent 调用。endpoint manifest 只发布路由信息；token 不写入 manifest、聊天快照或事件日志。sidecar 对
`/mcp` 请求校验 `Authorization: Bearer ...`，并拒绝非本机 HTTP `Origin`。这条能力只把 MCP 调用面
收窄到持有该 token 的调用方，不承担远程多用户 OAuth、权限分级或脚本沙箱职责。

```text
前端对话界面
  -> 前端投递会话
    -> 本机 CLI Agent 会话
      -> 已注册的相枢 MCP server
        -> 前端/后端插件 IPC 工具或脚本运行
        -> 前端对话消息 IPC 通知
```

## 玩家体验边界

玩家可见层始终是“玩家与相枢对话”。前端可以在内部维护 `session`、`origin` 等协议字段；界面呈现双方
已经发出的消息和必要的相枢文本说明。

可见交互遵循这些规则：

- 对话对象固定显示为“相枢”。
- 对话窗口渲染玩家和相枢已经发送出来的消息。
- `idle`、`replying`、`failed`、`reset` 等状态或事件驱动会话推进、发送按钮状态和必要的相枢说明。
- 玩家输入框承担自然语言输入；命令行、会话管理和工具调用细节留在内部链路。
- 运行边界和失败原因按日志策略进入游戏日志或 MCP sidecar 事件日志；可见失败说明仍由对话流承载。

## Agent 工作区与投递输入

默认打包目录预置一个自包含的 `DefaultAgentWorkspace/`，作为相枢的默认本机 Agent 工作区配置和自定义示范。
本节只记录运行时契约：CLI Agent 的进程工作目录就是这个工作区，运行时数据集中写入
`.xiangshu-runtime/`，默认工作区文件必须能在包内独立被读取。

默认工作区的读取路由由工作区入口文件和子目录 README 维护；源码维护时的资料来源、放置规则和快照核对路径见
`agent-context-sources.md`。运行设计文档不复制默认工作区的入口、资料和技能目录清单，只约束它们在对话链路
中的职责：入口指令提供基础相枢身份、口吻和玩家可见边界；资料和工具指引细化按需读取；Agent 技能目录由
具体 CLI 的发现机制读取。各 CLI Agent 使用哪个入口文件和技能目录由 `agent-cli-adapters.md` 维护。

`.xiangshu-notes/` 是可选本机工作记录目录，用于会话草稿和本地经验，默认包不创建它。`.xiangshu-runtime/`
是相枢前端插件和 MCP server 维护的运行数据目录，不属于可编辑 Agent 资产。

用户可以在该工作区手工维护自己的人设、世界观资料、工具指引、设置和 Agent 技能；运行中的 Agent 把这些
文件作为工作区配置读取。如果用户把 `AgentWorkingDirectory` 指向其它目录，该目录由用户自行维护。

内部投递给 CLI Agent 的每个投递输入只描述当前投递轮次的玩家消息。历史对话由 CLI Agent 自己的可恢复会话维护。

- `playerName`：当前玩家说话人的显示名，由前端读取当前太吾角色真实姓名。
- `playerMessages`：当前投递轮次待投递的玩家消息，按进入前端会话的顺序排列。每条消息包含 `id`、`sentAt` 和
  `content`。`id` 是当前前端会话内的消息句柄，格式为 `message-<ordinal>`；`sentAt` 写入 UTC 时间。

```json
{
  "playerName": "太吾",
  "playerMessages": [
    {
      "id": "message-1",
      "sentAt": "2026-06-16T12:34:56.789+00:00",
      "content": "且慢"
    }
  ]
}
```

相枢身份由默认工作区入口指令和游戏内显示层固定，不作为投递输入字段投递；扩展人设资料只细化表达。前端
捕获适配器返回的外部会话 id 后，在后续投递轮次通过对应 CLI 的 resume 参数恢复同一个外部会话。
首轮 CLI 调用如果没有返回可恢复会话 id，前端把它视为 CLI 协议失败，而不是降级成无上下文的后续投递。

前端要求 CLI Agent 的最终输出符合一个最小 JSON Schema：

```json
{
  "reply": "显示给玩家的相枢文本"
}
```

各适配器通过对应 CLI 支持的结构化输出参数传入同一 schema。前端提取 `reply` 写回会话；CLI 失败按日志策略
记录。需要告知玩家时，前端写入少量固定的相枢文本说明。具体参数见 `agent-cli-adapters.md`。

## Mod 运行数据目录

`AgentWorkingDirectory` 下的 `.xiangshu-runtime/` 是相枢 Mod 的运行数据目录。它位于 Agent 工作区内，
所有权属于相枢前端插件和 MCP server。默认工作区指令把它标为运行数据；创建、修改、删除和清理都由相枢
运行时完成。本节描述运行数据的所有权和恢复语义，文件格式是相枢运行时内部接口。

运行数据集中写入 `.xiangshu-runtime/`，避免和人设、世界观资料、运行工具指引、CLI 入口文件、Agent 技能
目录等可编辑资产混在一起。当前写入点：

- `ipc-endpoints.json`：前端、后端和 MCP server 共同使用的本机 endpoint manifest。
- `Diagnostics/McpServer/`：MCP sidecar 生命周期事件日志目录。
- `Temp/AgentCli/`：前端启动 CLI Agent 时使用的短生命周期协议文件目录，例如 JSON Schema、Codex
  last-message 和 MCP config；需要写入临时 MCP config 的适配器会在其中包含本次运行的 `Authorization: Bearer ...`
  header。单次调用结束后删除对应调用子目录。
- `ChatSessions/`：当前聊天会话选择和可恢复快照。文件格式是相枢前端的内部恢复数据。

## Mod 配置语义

太吾 Mod 用户配置提供这些字段：

- `AgentAdapter`：选择本机 CLI Agent 适配器；当前内置清单见 `agent-cli-adapters.md`。
- `AgentCliPath`：本机 Agent CLI 的命令名或可执行文件路径；留空时使用当前 Agent 的默认命令。
- `AgentWorkingDirectory`：本机 Agent 使用的工作目录，默认 `DefaultAgentWorkspace`。

默认命令按 `AgentAdapter` 决定。切换 Agent 类型时沿用同一个路径字段；本机 CLI 不在 PATH 或需要固定绝对
路径时填写 `AgentCliPath`。当前默认命令映射见 `agent-cli-adapters.md`。

`AgentWorkingDirectory` 使用相对路径时，插件会把它解析到相枢 Mod 目录下并创建目录。因此默认值
`DefaultAgentWorkspace` 对应：

```text
<Wanxiang.Xiangshu Mod directory>/DefaultAgentWorkspace
```

本地进阶设置使用相枢 Mod 目录下的本地 JSON 文件。它不进入太吾 Mod 配置界面，也不属于 Agent 工作区。
前端插件初始化时读取：

- `<Wanxiang.Xiangshu Mod directory>/LocalSettings.json`

当前支持 `agent.env` 对象。读取到的字符串键值只注入 CLI Agent 子进程，用于代理地址或 CLI
私有开关等本机配置，不注入游戏进程或 MCP sidecar，也不写入诊断日志。

太吾 Mod 用户配置和 `LocalSettings.json` 都属于启动参数。运行时继续使用本次启动时加载的值；重启游戏后，
前端和后端会按新配置重建 IPC endpoint、manifest 路径、MCP sidecar、前端 Agent 会话和 CLI 子进程环境。
游戏内修改 Mod 用户配置后，`OnModSettingUpdate` 只提示重启。

## 会话投递模型

玩家消息提交后，前端立即把它加入游戏内可见对话记录，并放入待投递队列。CLI Agent 空闲时，队列中的消息
组成一个投递轮次；CLI Agent 正在生成回复时，发送入口切换为“且慢”中断入口。

“且慢”会作为玩家消息进入可见对话记录，并取消当前 CLI 调用。它不会立刻单独投递；前端会暂停待投递队列，
直到玩家发出下一条普通消息，再把“且慢”和新消息一起组成下一轮输入。已经交给 CLI 的输入归 CLI 会话处理，
前端不撤回也不重放。

玩家重置聊天时，前端新建本地投递会话，清空可见消息、待投递队列和 `agentSessionId`。如果重置发生在 CLI
调用期间，前端取消当前调用，旧结果不会写回新会话。

前端投递会话的核心状态：

- `sessionId`：前端投递会话 id，由前端生成的 GUID-N 字符串。
- `adapter`：CLI 适配器持久化 key，用于判断恢复快照是否属于当前适配器。
- `agentSessionId`：CLI Agent 自己的可恢复会话 id；后续投递轮次用它恢复同一个本机 Agent 会话。
- `lastMessageOrdinal`：当前会话已分配的最后一个消息序号。
- `visibleMessages`：游戏内可见对话记录，供界面渲染和会话恢复使用。
- `pendingMessages`：已经显示给玩家、尚未进入投递轮次的玩家消息；“且慢”会暂停队列，直到下一条普通玩家
  消息触发投递。

持久化快照保存 `sessionId`、`adapter`、`agentSessionId`、`lastMessageOrdinal` 和 `visibleMessages`。
`pendingMessages` 是内存态。

跨游戏重启继续聊天依赖持久化的 `agentSessionId` 和可见消息快照；重启后前端会启动新的 MCP sidecar，
生成新的 bearer token，并在下一轮 CLI resume 调用中注入新的 endpoint 和 token。正在投递的 CLI 进程和
尚未进入投递轮次的 `pendingMessages` 不跨重启恢复。

对话消息使用同一种内部模型。状态和错误进入会话推进、按钮状态或少量相枢说明；显示层使用当前太吾真实
姓名与相枢两种身份：

- `id`：当前前端会话内的消息句柄，格式为 `message-<ordinal>`。
- `role`：`user` 或 `assistant`。
- `speakerName`：界面显示的说话人名称。
- `createdAt`：消息进入前端可见对话记录的 UTC 时间。
- `content`：消息文本；`role = "assistant"` 时显示为相枢消息，`role = "user"` 时保留玩家原文。
- `origin`：`user`、`agent`、`agent-intermediate` 或 `session`。`agent` 表示 CLI 最终 assistant 输出；
  `agent-intermediate` 表示 Agent 通过 MCP 中间答复工具写入的消息；`session` 表示前端会话写入的少量固定
  说明，例如适配器启动失败。

每个投递轮次对应一次 CLI Agent 调用。CLI 最终答复和 MCP 中间答复按产出顺序追加到可见对话记录。

## 对话界面映射

对话界面保持单一主交互。命令行、工具调用细节和脚本控制留在内部链路；界面渲染玩家和相枢已经发送出来的
消息：

- 玩家消息：玩家发送后立即追加到对话流。
- 相枢消息：在 CLI 最终答复、MCP 中间答复工具或前端固定说明产出文本时追加。
- `idle`：当前没有运行中的 CLI 调用，发送入口提交新玩家消息。
- `replying`：CLI 正在生成回复，发送按钮切换为可点击的“且慢”中断入口。
- `interrupted`：玩家点击“且慢”后，当前 CLI 调用被切断，发送入口回到普通输入；前端等待下一条普通玩家
  消息触发后续投递。
- `failed`：如果需要让玩家知道失败，前端会话追加一条 `origin = "session"` 的相枢固定文本说明；失败细节
  按日志策略处理。
- `reset`：玩家手动重置时，前端清空可见聊天消息并新建本地会话。

错误说明以相枢消息进入对话流；运行边界和失败原因按日志策略处理。前端固定说明在界面上显示为相枢气泡，
元数据保留 `origin = "session"`。

界面身份表达保留两个锚点：窗口头部显示相枢身份，消息气泡内显示说话人名称。玩家通过热键、输入框、
发送按钮和已有对话流理解主交互。

## IPC 脚本运行

脚本调用从 MCP 工具进入相枢本机 IPC，再在目标插件进程内执行。目标侧由工具参数选择；前端和后端各自只
暴露本进程可访问的游戏 API、运行状态和线程边界。

```text
CLI Agent
  -> MCP tool
    -> MCP server proxy
      -> 前端或后端 IPC 脚本请求
        -> 前端或后端脚本运行器
          -> MCP 工具返回
```

模块边界按所有权划分：

- `src/Ipc/` 定义跨进程请求/响应 contract。脚本响应只表达运行事实：
  `notInvoked(reason)` 或 `invoked(returnValue | exception)`。
- `src/McpServer/` 拥有 MCP 工具语义：选择目标 endpoint、转发 IPC 请求，并把内部响应整理成 Agent 可读
  JSON。
- `src/Scripting/` 提供前端和后端共用的受信 C# 编译与执行器。
- `src/Frontend/Ipc/` 暴露前端侧脚本执行能力，负责前端进程可访问的游戏 API、界面上下文和前端运行状态。
- `src/Backend/` 暴露后端侧脚本执行能力，负责后端进程可访问的游戏 API 和后端运行状态。
- 会修改游戏状态的能力落在实际承载该 API 和线程边界的前端或后端插件中。
- 玩家可见文本不属于脚本执行 contract；Agent 可以走最终答复，或显式调用中间答复工具。

MCP 工具返回带 discriminator 的 sum type。它只说明脚本入口调用事实，不替 Agent 判断玩家目标是否达成：

```json
{ "kind": "notInvoked", "reason": "..." }
{ "kind": "invoked", "outcome": { "kind": "returnValue", "value": {} } }
{ "kind": "invoked", "outcome": { "kind": "exception", "message": "..." } }
```

`kind = "notInvoked"` 表示入口方法未被调用，例如引用失败、编译错误、入口契约不满足或调用前取消。
`kind = "invoked"` 表示入口方法已被调用，`outcome.kind` 再区分入口返回值和运行时异常/异步取消。编译
warning 不进入工具返回。

脚本以完全信任方式在目标插件进程内运行，不提供沙箱。稳定读写游戏状态的 facade 由前后端模块按侧端能力
扩展；编译引用和临时程序集依赖解析规则由 `src/Scripting/README.md` 维护。

MCP 工具形态：

```text
xiangshu_run_csharp_script(
  targetSide,
  script,
  argumentsJson
)
```

参数语义：

- `targetSide`：目标插件侧，取 `frontend` 或 `backend`。
- `script`：完整 C# 编译单元。
- `argumentsJson`：可选 JSON object，转为 `XiangshuScriptGlobals.Arguments`。

脚本通道传递完整 C# 编译单元，不定义 statements/expression 模式，也不提供预置 `using` 列表。脚本自己
声明 `using`、类型和入口；入口契约由 `src/Scripting/README.md` 维护。入口契约不满足时，运行器返回
`notInvoked(reason)`，不把它归类为 MCP 或 IPC 转发失败。

## MCP 驱动的中间答复

MCP server 提供一个供 Agent 调用的中间答复工具，用于在最终 `reply` 返回前追加一条玩家可见的相枢文本。
当前投递轮次的最终答复仍由 CLI 调用结果收束。

工具形态：

```text
xiangshu_send_intermediate_reply(
  content
)
```

参数语义：

- `content`：显示给玩家的短文本。

MCP server 收到工具调用后，通过本机 IPC 通知前端。前端当前聊天会话负责追加对话消息；可见文本由调用
该工具的 Agent 提供。需要交代给 Agent 的前端侧事件由投递模型中的前端事实字段承载；长期对话记录和
Agent 调用仍归前端投递会话与本机 CLI 会话。

中间答复工具的典型用途：

- 长时间任务开始前，相枢先说明接下来会做什么。
- 相枢已经发现一个可见中间结果，但最终答复还需要继续等待。
- 工具调用链出现可恢复的临时失败，前端需要先让玩家看到原因。

## CLI 适配器边界

本节只记录对话链路依赖的适配器边界：前端把一个投递轮次交给本机 CLI Agent，等待最终答复或协议失败；
MCP 工具归 MCP server，游戏状态修改归前端或后端脚本能力。

前端启动 CLI 进程时，把 `AgentWorkingDirectory` 设为进程工作目录；该目录是本机 Agent 的受信工作区。
CLI 启动参数负责选择完全信任式非交互模式，并传入会话恢复、MCP 配置和结构化输出约束。聊天 UI 只接收
最终相枢文本或失败说明；如果 CLI 因环境约束异常退出、被阻断或没有返回所需协议字段，前端会话按 `failed`
映射成相枢文本答复。

当前适配器清单、默认命令、工作区入口、会话 id 来源、最终答复来源和命令形态由 `agent-cli-adapters.md`
维护。新增 Agent 只有改变本章描述的投递模型、运行数据所有权或玩家可见行为时，才需要修改本文件。

当前适配使用进程边界处理回合控制。玩家“且慢”通过取消 CLI 进程实现。长连接 transport、SDK streaming 或
ACP 等其它接入方式实际改接时再进入适配器设计。

## 迭代边界

对话入口、CLI 适配、MCP sidecar、前后端脚本通道和中间答复工具属于同一条本机 Agent 对话路径。后续扩展
优先补齐前端和后端各自稳定的脚本 facade，避免 Agent 长期依赖零散游戏内部类型。

会修改游戏状态的 MCP 能力继续通过受信脚本运行通道承载，并按实际 API 和线程边界归属到前端或后端插件。
