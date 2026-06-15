# 相枢对话与本机 Agent 内部设计

## 目标形态

相枢的对话入口由前端插件负责。玩家界面呈现为与 NPC “相枢”的连续对话；前端负责游戏内消息、投递状态
和 CLI 调用，本机 Agent 自己维护长期会话上下文。交互以流畅度优先：前端启动 CLI Agent 时默认使用各
CLI 的完全信任式非交互执行方式，让一次游戏内投递轮次收束为一条相枢最终回应。
适配对象：

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
- `idle`、`working`、`failed`、`reset` 等内部状态驱动会话推进、发送按钮状态和必要的相枢说明。
- 玩家输入框承担自然语言输入；命令行、会话管理和工具调用日志留在内部链路。
- 调试细节进入游戏日志或 MCP sidecar 事件日志；日志边界见 `logging.md`。

## Agent 工作区与回合输入

默认打包目录预置一个自包含的 `AgentWorkspace/`，作为相枢的默认本机 Agent 工作区配置和自定义示范。
默认工作区按文件职责拆分：

- `AGENTS.md` 只补充默认对话上下文、玩家可见边界、事实来源、运行目录所有权和最终输出契约。
- `GAME_CONTEXT.md` 提供轻量静态太吾语境；当前角色、地点、界面和运行状态仍来自本轮输入或工具结果。
- `.agents/skills/` 与 `.claude/skills/` 是对应 CLI Agent 自动发现的技能目录；技能触发边界和执行指导由
  各自 `SKILL.md` 维护，不由 `AGENTS.md` 复制。
- `.xiangshu-runtime/` 是运行数据目录，由前端插件和 MCP server 维护，不属于可编辑 Agent 资产。

用户可以在该工作区维护自己的人设、上下文、设置和 Agent 技能。如果用户把 `AgentWorkingDirectory` 指向
其它目录，该目录由用户自行维护。

内部投递给 CLI Agent 的每轮输入使用结构化回合输入：

- `participants`：玩家和相枢两个说话人；玩家名由前端读取当前太吾角色真实姓名。
- `currentPlayerMessages`：本轮投递给 Agent 的玩家消息。

结构化回合输入描述当前轮次。前端捕获 Codex `thread_id` 或 Claude Code `session_id` 后，在后续轮次通过
CLI resume 参数恢复同一个外部会话；历史上下文由本机 Agent 会话提供。

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
所有权属于相枢前端插件和 MCP server。`AGENTS.md` 要求 Agent 不创建、修改、删除或整理
`.xiangshu-runtime/`。本节描述运行数据的所有权和恢复语义，不把这些文件格式作为外部稳定接口。

运行数据集中写入 `.xiangshu-runtime/`，避免和工作区上下文、轻量语境、CLI 入口兼容文件、Agent 技能目录
等可编辑资产混在一起。

运行时写入点：

- `ipc-endpoints.json`：前端、后端和 MCP server 共同使用的本机 endpoint manifest。
- `Diagnostics/McpServer/`：MCP sidecar 事件日志目录。
- `Temp/AgentCli/`：前端启动 Codex/Claude 时使用的短生命周期协议文件目录，例如
  `--output-last-message`、`--output-schema` 和 `--mcp-config` 所需文件；单次调用结束后删除对应
  调用子目录。
- `ChatSessions/`：当前聊天会话目录，用于保存当前会话选择、游戏内可见聊天记录和外部 Agent 会话 id。
  `sessions/` 保存当前可恢复快照。

这些写入点集中在 `.xiangshu-runtime/` 下，让 Agent 工作区根目录保持稳定，也让 Agent 更容易识别相枢
运行数据的所有权。

`ChatSessions/current.json` 是当前会话选择文件，由前端读取和写回。它记录更新时间和
`currentSessionId`。文件不存在表示没有可恢复会话。

`ChatSessions/sessions/<session-id>.json` 代表一个前端投递会话，`session-id` 为前端生成的 GUID-N。
它记录前端会话 id、所选适配器、外部 Agent 会话 id、消息计数器，以及游戏内可见消息。前端启动时恢复
适配器与当前配置一致的会话；适配器不一致时，前端创建新的空会话。

会话文件描述前端需要维护的事实：当前会话、外部会话 id、消息计数器和游戏内可见消息。Agent 长期上下文
由 CLI 会话维护；工具调用和 MCP 事件日志归各自日志链路。`pendingMessages` 保留为内存态；若当前会话
已恢复，玩家继续输入时会从恢复的外部 Agent 会话 id 继续新的轮次。

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
私有开关等本机配置，不注入游戏进程或 MCP sidecar，也不写入日志。

后续如果需要更多不适合进入太吾 Mod 配置界面的本地进阶设置，优先在这个文件中新增明确的顶层配置段；只有
设置的生命周期、所有者或可见性明显不同，才新增独立文件。

太吾 Mod 用户配置和 `LocalSettings.json` 都属于启动参数。运行时继续使用本次启动时加载的值；重启游戏后，
前端和后端会按新配置重建 IPC endpoint、manifest 路径、MCP sidecar、前端 Agent 会话和 CLI 子进程环境。
游戏内修改 Mod 用户配置后，`OnModSettingUpdate` 只提示重启。

## 会话投递模型

玩家常规消息进入前端投递会话后立即写入游戏内可见对话记录。CLI Agent 空闲时，消息立即进入一个
投递轮次；CLI Agent 正在工作时，常规发送入口切换为中断入口。

玩家点击忙碌态发送按钮后，前端追加一条固定玩家消息“且慢”，并取消当前 CLI Agent 调用。前端会把已经
投递但尚未完成的当前轮玩家消息与“且慢”一起放回 `pendingMessages`；当前 CLI 进程结束后，这些消息作为
下一轮重新投递。这样即使当前 CLI 进程没有保存本轮上下文，Agent 仍能在新一轮输入里看到被中断的玩家消息
和“且慢”。

前端通过 `agentSessionId` 恢复本机 Agent 自己的会话；结构化回合输入承载本轮玩家消息和参与者信息。

玩家也可以通过聊天窗口头部左侧的重置入口手动清空当前可见聊天消息。手动重置会新建本地投递会话、
清空 `agentSessionId` 和等待投递的玩家消息。若手动重置发生在 CLI 调用期间，前端会取消当前轮，避免
旧轮结果写回新会话。

前端投递会话可以按这些核心字段理解。实现保存最小运行所需的数据：

- `sessionId`：前端投递会话 id，由前端生成的 GUID-N 字符串。
- `adapter`：`codex` 或 `claude`。
- `agentSessionId`：CLI Agent 自己的可恢复会话 id；后续轮次用它恢复同一个本机 Agent 会话。
- `visibleMessages`：游戏内可见对话记录，服务界面渲染和前端元数据。
- `pendingMessages`：已经显示给玩家、尚未进入投递轮次，或因当前轮被中断而等待重投递的玩家消息。

持久化快照保存 `sessionId`、`adapter`、`agentSessionId`、消息计数器和 `visibleMessages`；
`pendingMessages` 是内存态。

对话消息使用同一种内部模型。状态和错误进入会话推进、按钮状态或少量相枢说明；显示层使用当前太吾真实
姓名与相枢两种身份。内部仍可使用 `assistant` 作为协议角色名，但显示层必须把它映射为“相枢”：

- `role`：`user` 或 `assistant`。
- `content`：消息文本；`role = "assistant"` 时显示为相枢消息，`role = "user"` 时保留玩家原文。
- `origin`：`user`、`agent`、`agent-intermediate` 或 `session`。`agent` 表示 CLI 最终 assistant 输出；
  `agent-intermediate` 表示 Agent 通过 MCP 中间答复工具写入的消息；`session` 表示前端会话写入的少量固定
  说明，例如适配器启动失败。

每个投递轮次对应一次 CLI Agent 调用。当前轮次携带本轮待投递的玩家消息；assistant 答复和中间答复按
产出顺序追加到可见对话记录。玩家发起中断后，当前轮玩家消息会与“且慢”一起重新形成下一轮。后续如果
需要把前端侧故障、状态压缩说明或更严格的中间答复轮次归属交给 Agent，应使用独立的前端事实字段描述
这些事件；可见对话记录仍保持为界面层数据。

## 对话界面映射

对话界面保持单一主交互。命令行、工具日志和脚本控制留在内部链路；界面渲染玩家和相枢已经发送出来的
消息：

- 玩家消息：玩家发送后立即追加到对话流。
- 相枢消息：在 CLI 最终答复、MCP 中间答复工具或前端固定说明产出文本时追加。
- `idle`：当前没有运行中的 CLI 调用，发送入口提交新玩家消息。
- `working`：发送按钮切换为可点击的“且慢”中断入口。
- `interrupting`：玩家点击“且慢”后，按钮进入止息状态，直到当前 CLI 调用退出并开始下一轮。
- `failed`：如果需要让玩家知道失败，前端会话追加一条 `origin = "session"` 的相枢固定文本说明；失败细节
  按日志策略处理。
- `reset`：玩家手动重置时，前端清空可见聊天消息并新建本地会话。

错误说明以相枢消息进入对话流；技术诊断细节按日志策略处理。

这种固定说明在界面上显示为相枢气泡，但元数据必须保留 `origin = "session"`。如果后续需要让 Agent
了解这类前端侧事件，应通过明确的前端事实字段投递。

界面身份表达保留两个锚点：窗口头部显示相枢身份，消息气泡内显示说话人名称。玩家通过热键、输入框、
发送按钮和已有对话流理解主交互。

## IPC 脚本运行

脚本调用通过 MCP 工具进入目标侧 IPC endpoint，再由目标插件进程内的脚本运行器执行。目标路径是：

```text
CLI Agent
  -> MCP tool
    -> MCP server proxy
      -> 前端或后端 IPC 脚本请求
        -> 前端或后端脚本运行器
          -> MCP 工具返回
```

边界按所有权划分：

- `src/Ipc/` 定义脚本运行请求、入口返回值、错误和诊断 contract。
- `src/McpServer/` 把 MCP 工具调用转成目标侧 IPC 请求，并把入口返回值、错误和诊断整理为 Agent 可读的
  工具返回。
- `src/Scripting/` 提供前端和后端共用的受信 C# 编译与执行器。
- `src/Frontend/Ipc/` 暴露前端侧脚本执行能力，负责前端进程可访问的游戏 API、界面上下文和前端运行状态。
- `src/Backend/` 暴露后端侧脚本执行能力，负责后端进程可访问的游戏 API 和后端运行状态。
- 会修改游戏状态的脚本能力落在实际承载该 API 和线程边界的前端或后端插件中。
- 玩家可见文本不属于脚本执行 contract；Agent 可以走最终答复，或显式调用中间答复工具。

脚本通道覆盖“提交脚本、执行、返回入口返回值 JSON/错误/诊断”。脚本以完全信任方式在目标插件进程内运行，
编译时引用该进程已加载且有物理路径的程序集；稳定读写游戏状态的 facade 由前后端模块按侧端能力扩展。

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
- `script`：完整 C# 编译单元，入口契约见下文。
- `argumentsJson`：可选 JSON object，转为 `XiangshuScriptGlobals.Arguments`。

脚本通道传递完整 C# 编译单元，不定义 statements/expression 模式，也不提供预置 `using` 列表。脚本自己
声明 `using`、类型和入口；runner 只负责编译源码，并调用名为 `XiangshuScript` 的 public static 非泛型
class 上的 public static `Execute` 或 `ExecuteAsync` 方法。入口接收一个 `XiangshuScriptGlobals` 参数，
用来访问目标侧、调用参数和取消信号。若入口契约不满足，runner 返回统一的入口契约错误，而不是把它归类为
MCP 或 IPC 转发失败。

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
`AGENTS.md` 不承担技能触发说明；MCP 工具是否可用、参数和副作用由 MCP server 的工具 metadata 暴露。

适配器启动 CLI 进程时，必须把进程工作目录设置为 `AgentWorkingDirectory`。进程工作目录是主工作区
来源；命令行参数补充各 CLI 额外需要的会话参数。

适配器默认使用各 CLI 的完全信任式非交互参数。`AgentWorkingDirectory` 是本机 Agent 的受信工作区；
权限/信任选择在 CLI 启动参数中完成，聊天 UI 接收最终 JSON 答复或失败说明。如果 CLI 因环境约束异常
退出或被阻断，前端会话按 `failed` 映射成相枢文本答复。

`codex exec` 与 `claude --print` 适配使用进程边界处理回合控制。玩家“且慢”会取消当前轮、结束
CLI 进程，并把上下文重投递给下一轮。Codex app-server 的
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
