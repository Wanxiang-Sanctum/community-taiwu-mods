# 相枢对话与本机 Agent 内部设计

## 目标形态

相枢的对话入口由前端插件负责。玩家界面呈现为与 NPC “相枢”的连续对话；前端负责游戏内消息、投递状态
和 CLI 调用，本机 Agent 自己维护长期会话上下文。交互以流畅度优先：前端启动 CLI Agent 时默认使用各
CLI 的完全信任式非交互执行方式，让一次游戏内投递轮次收束为一条相枢最终回应。当前适配对象：

- Codex CLI
- Claude Code

游戏内投递协议归前端会话所有；长期上下文归本机 Agent 会话所有。MCP server 的职责是作为相枢暴露给
本机 Agent 的工具服务：前端在启动 CLI Agent 时，把当前相枢 MCP endpoint 注册给 Agent；Agent 需要
读取或操作相枢能力时，再通过 MCP 调用相枢工具。

MCP server 可以额外提供 Agent 中间答复工具。Agent 调用这个工具时，MCP server 在内部处理后通过 IPC
通知前端追加一条相枢消息。中间答复是同一轮 CLI 调用最终答复之前的可见回应入口；最终答复仍由 CLI
调用结果收束。

```text
前端对话界面
  -> 前端投递会话
    -> Codex CLI / Claude Code 会话
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

## Agent 工作区与回合输入

默认打包目录预置一个自包含的 `AgentWorkspace/`，作为相枢的默认本机 Agent 工作区配置和自定义示范。
默认工作区相关内容按职责拆分：

- `AGENTS.md` 只补充默认对话上下文、玩家可见边界、事实来源、运行目录所有权和最终输出契约。
- `context/README.md` 是静态语境路由入口，按本轮需要指向人设、基础世界观和相枢深入资料；当前角色、
  地点、界面和运行状态仍来自本轮输入或工具结果。
- `context/` 存放语境入口和按需读取的世界观/人设叶子文件，避免把相枢口吻、基础概念和深入资料堆进同一个
  上下文文件。这个目录随 Mod 发布后应保持自包含，不依赖源码仓库或开发机上的游戏镜像路径。
- `capabilities/README.md` 是运行能力上下文路由入口，按本轮实际可用工具和玩家目标指向脚本能力说明或游戏
  知识检索说明。
- `capabilities/` 存放按需读取的运行能力上下文，例如默认脚本工具的目标侧选择、入口契约、运行时锚点，以及
  配置、本地化、模板显示辅助和百晓册资料的直接检索路线。
  这些文件随 Mod 发布后应保持自包含；工具是否实际存在、参数名称和副作用仍以本轮工具说明为准。
- `.agents/skills/` 与 `.claude/skills/` 是对应 CLI Agent 自动发现的技能目录；技能触发边界和执行指导由
  各自 `SKILL.md` 维护，不由 `AGENTS.md` 复制。
- `.xiangshu-notes/` 是可选的本机 Agent 工作记录目录，默认包不创建它。它把临时备注从 `context/` 和
  `.xiangshu-runtime/` 中分离出来。
- `.xiangshu-runtime/` 是运行数据目录，由前端插件和 MCP server 维护，不属于可编辑 Agent 资产。

用户可以在该工作区手工维护自己的人设、上下文、设置和 Agent 技能；运行中的 Agent 把这些文件作为工作区
配置读取。如果用户把 `AgentWorkingDirectory` 指向其它目录，该目录由用户自行维护。

内部投递给 CLI Agent 的每轮输入只描述本轮玩家消息。历史对话由 CLI Agent 自己的可恢复会话维护。

- `playerName`：当前玩家说话人的显示名，由前端读取当前太吾角色真实姓名。
- `playerMessages`：本轮待投递的玩家消息，按进入前端会话的顺序排列。每条消息包含 `id`、`sentAt` 和
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

相枢身份由默认工作区指令和游戏内显示层固定，不作为回合输入字段投递。前端捕获 Codex `thread_id` 或
Claude Code `session_id` 后，在后续轮次通过 CLI resume 参数恢复同一个外部会话。

前端要求 CLI Agent 的最终输出符合一个最小 JSON Schema：

```json
{
  "reply": "显示给玩家的相枢文本"
}
```

Codex 适配器通过 `--output-schema` 传入 schema 文件；Claude Code 适配器通过 `--json-schema` 传入同一
schema。前端提取 `reply` 写回会话；CLI 失败按日志策略记录。需要告知玩家时，前端写入少量固定的相枢文本
说明。

## Mod 运行数据目录

`AgentWorkingDirectory` 下的 `.xiangshu-runtime/` 是相枢 Mod 的运行数据目录。它位于 Agent 工作区内，
所有权属于相枢前端插件和 MCP server。默认工作区指令把它标为运行数据；Agent 不应创建、修改、删除或
整理其中内容。本节描述运行数据的所有权和恢复语义，不把这些文件格式作为外部稳定接口。

运行数据集中写入 `.xiangshu-runtime/`，避免和工作区上下文、静态语境、CLI 入口兼容文件、Agent 技能目录
等可编辑资产混在一起。当前写入点：

- `ipc-endpoints.json`：前端、后端和 MCP server 共同使用的本机 endpoint manifest。
- `Diagnostics/McpServer/`：MCP sidecar 生命周期事件日志目录。
- `Temp/AgentCli/`：前端启动 Codex/Claude 时使用的短生命周期协议文件目录，例如
  `--output-last-message`、`--output-schema` 和 `--mcp-config` 所需文件；单次调用结束后删除对应
  调用子目录。
- `ChatSessions/`：当前聊天会话选择和可恢复快照。文件格式是相枢前端的内部恢复数据，不作为外部稳定接口。

## Mod 配置语义

太吾 Mod 用户配置提供这些字段：

- `AgentAdapter`：选择 `Codex CLI` 或 `Claude Code`。
- `AgentCliPath`：本机 Agent CLI 的命令名或可执行文件路径；留空时使用当前 Agent 的默认命令。
- `AgentWorkingDirectory`：本机 Agent 使用的工作目录，默认 `AgentWorkspace`。

默认命令按 `AgentAdapter` 决定：Codex CLI 使用 `codex`，Claude Code 使用 `claude`。切换 Agent 类型时
沿用同一个路径字段；本机 CLI 不在 PATH 或需要固定绝对路径时填写 `AgentCliPath`。

`AgentWorkingDirectory` 使用相对路径时，插件会把它解析到相枢 Mod 目录下并创建目录。因此默认值
`AgentWorkspace` 对应：

```text
<Wanxiang.Xiangshu Mod directory>/AgentWorkspace
```

本地进阶设置使用相枢 Mod 目录下的本地 JSON 文件。它不进入太吾 Mod 配置界面，也不属于 Agent 工作区。
前端插件初始化时读取：

- `<Wanxiang.Xiangshu Mod directory>/LocalSettings.json`

当前支持 `agent.env` 对象。读取到的字符串键值只注入 Codex/Claude CLI 子进程，用于代理地址或 CLI
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
- `adapter`：`codex` 或 `claude`。
- `agentSessionId`：CLI Agent 自己的可恢复会话 id；后续轮次用它恢复同一个本机 Agent 会话。
- `lastMessageOrdinal`：当前会话已分配的最后一个消息序号。
- `visibleMessages`：游戏内可见对话记录，供界面渲染和会话恢复使用。
- `pendingMessages`：已经显示给玩家、尚未进入投递轮次的玩家消息；“且慢”会暂停队列，直到下一条普通玩家
  消息触发投递。

持久化快照保存 `sessionId`、`adapter`、`agentSessionId`、`lastMessageOrdinal` 和 `visibleMessages`。
`pendingMessages` 是内存态。

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
同一轮的最终答复仍由 CLI 调用结果收束。

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

适配器的边界是“把一个轮次交给真实本机 CLI Agent，并返回最终答复”。MCP 工具归 MCP server，游戏状态
修改归前端或后端脚本能力。

每个适配器接收同一组调用参数：

- CLI 入口。
- 工作目录。
- 相枢 MCP endpoint。
- 可选的外部会话 id；存在时必须恢复同一个 CLI Agent 会话。
- 当前投递轮次的结构化回合输入。
- 最终回复 JSON Schema。

工作目录同时是 CLI Agent 的工作区根目录；其中的入口指令、设置和 Agent 技能由 CLI Agent 自行加载。
`AGENTS.md` 不承担技能触发说明，也不维护工具清单；MCP 工具是否可用、参数和副作用由本轮暴露的工具说明
决定。

适配器启动 CLI 进程时，必须把进程工作目录设置为 `AgentWorkingDirectory`。进程工作目录是主工作区
来源；命令行参数补充各 CLI 额外需要的会话参数。

适配器默认使用各 CLI 的完全信任式非交互参数。`AgentWorkingDirectory` 是本机 Agent 的受信工作区；
权限/信任选择在 CLI 启动参数中完成，聊天 UI 接收最终 JSON 答复或失败说明。如果 CLI 因环境约束异常
退出或被阻断，前端会话按 `failed` 映射成相枢文本答复。

`codex exec` 与 `claude --print` 适配使用进程边界处理回合控制。玩家“且慢”当前通过取消 CLI 进程实现。
Codex app-server 的
`turn/interrupt` 和 Claude Code SDK streaming 的 `interrupt()` 属于不同 transport，实际改接时再进入
适配器。

下面记录适配器使用的命令形态。

### Codex

Codex 适配器使用 `--dangerously-bypass-approvals-and-sandbox` 进入完全信任式非交互模式。首次调用时
设置 `WorkingDirectory = AgentWorkingDirectory`，同时传入 `--cd <workingDirectory>`，让 Codex 的项目
根与进程工作目录保持一致：

```text
codex exec --dangerously-bypass-approvals-and-sandbox --skip-git-repo-check --json --output-last-message <file> --output-schema <schema-file> --cd <workingDirectory> -
```

结构化回合输入通过 stdin 传入。前端会话从 Codex JSONL 事件里的 `thread.started.thread_id` 捕获
`agentSessionId`，并优先读取 `--output-last-message` 指定临时文件中的最终答复。

后续调用使用捕获到的 `agentSessionId` 恢复同一个 Codex 会话：

```text
codex exec resume --dangerously-bypass-approvals-and-sandbox --skip-git-repo-check --json --output-last-message <file> --output-schema <schema-file> --config 'mcp_servers.xiangshu.url="http://127.0.0.1:<port>/mcp"' <agentSessionId> -
```

注册相枢 MCP endpoint 时，适配器通过 `--config` 临时传入：

```text
mcp_servers.xiangshu.url="http://127.0.0.1:<port>/mcp"
```

### Claude Code

Claude Code 适配器使用 print mode，并通过 `--dangerously-skip-permissions` 进入完全信任式非交互模式。
Claude Code 的主工作区来自进程工作目录，因此启动进程时设置 `WorkingDirectory = AgentWorkingDirectory`：

```text
claude --print --output-format stream-json --verbose --dangerously-skip-permissions --mcp-config <file> --json-schema <schema-json> <agent-input>
```

适配器从 stream-json 中捕获 `session_id`，并从 result event 的 `structured_output` 提取结构化最终答复；
未使用结构化输出时再回退读取非空 `result`。

后续调用使用捕获到的 `session_id` 恢复同一个 Claude Code 会话：

```text
claude --print --resume <session_id> --output-format stream-json --verbose --dangerously-skip-permissions --mcp-config <file> --json-schema <schema-json> <agent-input>
```

注册相枢 MCP endpoint 时，适配器写入临时 `--mcp-config` JSON：

```json
{
  "mcpServers": {
    "xiangshu": {
      "type": "http",
      "url": "http://127.0.0.1:<port>/mcp"
    }
  }
}
```

如果后续需要让 Claude Code 访问主工作区之外的目录，再额外使用 `--add-dir <path>`；主工作区仍由进程
工作目录提供。

## 迭代边界

对话入口、CLI 适配、MCP sidecar、前后端脚本通道和中间答复工具属于同一条本机 Agent 对话路径。后续扩展
优先补齐前端和后端各自稳定的脚本 facade，避免 Agent 长期依赖零散游戏内部类型。

会修改游戏状态的 MCP 能力继续通过受信脚本运行通道承载，并按实际 API 和线程边界归属到前端或后端插件。
