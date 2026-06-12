# 相枢对话与本机 Agent 内部设计

## 目标形态

相枢的对话入口由前端插件负责。玩家界面呈现为与 NPC “相枢”的连续对话；前端负责游戏内消息、投递队列
和 CLI 调用，本机 Agent 自己维护长期会话上下文。交互以流畅度优先：前端启动 CLI Agent 时默认使用各
CLI 的完全信任式非交互执行方式，避免游戏内对话等待权限交互，让一次玩家输入对应一次完整的相枢回应。
当前接入两个适配对象：

- Codex CLI
- Claude Code

游戏内投递协议归前端会话所有；长期上下文归本机 Agent 会话所有。MCP server 的职责是作为相枢暴露给
本机 Agent 的工具服务：前端在启动 CLI Agent 时，把当前相枢 MCP endpoint 注册给 Agent；Agent 需要
读取或操作相枢能力时，再通过 MCP 调用相枢工具。

MCP server 可以额外提供 Agent 中间答复工具。Agent 调用这个工具时，MCP server 在内部处理后通过 IPC
通知前端追加一条相枢消息。中间答复是同一轮 CLI 调用最终答复之前的可见回应入口；它不承诺更快，也不
替代最终答复。

```text
前端对话界面
  -> 前端投递会话
    -> Codex CLI / Claude Code 会话
      -> 已注册的相枢 MCP server
        -> 前端/后端插件 IPC 工具
        -> 前端对话消息 IPC 通知
```

## 玩家体验边界

玩家可见层始终是“玩家与相枢对话”。前端可以在内部维护 `session`、`origin` 等
协议字段；界面只呈现双方已经发出的消息和必要的相枢文本说明。

可见交互遵循这些规则：

- 对话对象固定显示为“相枢”。
- 对话窗口只渲染玩家和相枢已经发送出来的消息。
- `working`、`queued`、`failed`、`ended` 等内部状态只驱动会话推进和必要的相枢说明。
- 玩家输入框承担自然语言输入；命令行、会话管理和工具调用日志留在内部链路。
- 调试细节进入游戏日志、MCP server 事件日志或诊断入口。

## Agent 工作区与回合输入

默认打包目录预置 `AgentWorkspace/AGENTS.md`，作为相枢的默认本机 Agent 工作区配置和自定义示范。
`AgentWorkspace/CLAUDE.md` 只负责让 Claude Code 转向同目录的 `AGENTS.md`，避免维护两份默认人设。
用户可以在该工作区维护自己的人设、指令、设置和 Agent 技能。如果用户把 `AgentWorkingDirectory`
指向其它目录，该目录由用户自行维护。

内部投递给 CLI Agent 的每轮输入使用结构化回合输入：

- `participants`：玩家和相枢两个说话人；玩家名由前端读取当前太吾角色真实姓名。
- `currentPlayerMessages`：本轮投递给 Agent 的玩家消息。

结构化回合输入只描述当前轮次，不携带前端会话 id、CLI 会话 id 或工具调用记录。前端捕获 Codex
`thread_id` 或 Claude Code `session_id` 后，在后续轮次通过 CLI resume 参数恢复同一个外部会话；
历史上下文由本机 Agent 会话提供。

前端要求 CLI Agent 的最终输出符合一个最小 JSON Schema：

```json
{
  "reply": "显示给玩家的相枢文本"
}
```

Codex 适配器通过 `--output-schema` 传入 schema 文件；Claude Code 适配器通过 `--json-schema` 传入同一
schema。前端只提取 `reply` 写回会话；进程错误、退出码、标准错误和回复解析失败只进入游戏日志。需要
告知玩家时，前端写入少量固定的相枢文本说明。

## Mod 运行数据目录

`AgentWorkingDirectory` 下的 `XiangshuRuntime/` 是相枢 Mod 的运行数据目录。它位于 Agent 工作区内，
但所有权属于相枢前端插件和 MCP server，不是 Agent 技能目录，也不是 Agent 应整理、改写或清理的
工作内容。默认 `AGENTS.md` 只说明这个所有权边界：`XiangshuRuntime/` 由相枢 Mod 维护，Agent 不创建、
不修改、不删除其中内容。具体文件格式留给前端实现和设计文档，不写入 `AGENTS.md`。

目录按当前写入点组织为：

```text
AgentWorkspace/
  AGENTS.md
  CLAUDE.md
  XiangshuRuntime/
    README.md
    ipc-endpoints.json
    Diagnostics/
      McpServer/
    Temp/
      AgentCli/
    ChatSessions/
      current.json
      sessions/
        <session-id>.json
```

当前运行时写入：

- `ipc-endpoints.json`：前端、后端和 MCP server 共同使用的本机 endpoint manifest。
- `Diagnostics/McpServer/`：MCP sidecar 的事件日志目录。
- `Temp/AgentCli/`：前端启动 Codex/Claude 时使用的短生命周期协议文件目录，例如
  `--output-last-message`、`--output-schema` 和 `--mcp-config` 所需文件；单次调用结束后删除对应
  调用子目录。
- `ChatSessions/`：当前聊天会话目录，用于保存当前会话选择、游戏内可见聊天记录和外部 Agent 会话 id。

这些写入点集中在 `XiangshuRuntime/` 下，避免相枢 Mod 在 Agent 工作区根目录直接写入可变文件，也让
Agent 更容易识别哪些内容不属于自己维护。

`ChatSessions/current.json` 是当前会话选择文件，由前端读取和写回。它记录更新时间和
`currentSessionId`。文件不存在表示没有可恢复会话。

`ChatSessions/sessions/<session-id>.json` 代表一个前端投递会话，`session-id` 为前端生成的 GUID-N。
它记录前端会话 id、所选适配器、外部 Agent 会话 id、消息计数器，以及游戏内可见消息。前端启动
时只恢复适配器与当前配置一致的会话；适配器不一致时，前端创建新的空会话，不恢复旧可见消息，也不
复用旧外部 Agent 会话 id。

会话文件只描述前端需要维护的事实，不复制 Agent 自己的长期上下文，不保存工具调用日志，也不把 MCP
事件日志混入聊天记录。前端当前不持久化 `pendingMessages`，因此重启后不会自动补投上次关闭时尚未进入
CLI 调用的排队消息；若当前会话已恢复，玩家继续输入时会从恢复的外部 Agent 会话 id 继续新的轮次。

## 当前实现边界

当前迭代已经铺设太吾 Mod 用户配置、MCP sidecar、聊天热键和最小对话入口。前端源码也按运行职责拆分：
`Agent/` 负责本机 Agent 调用，`Chat/` 负责会话和窗口，`HotKeys/` 负责热键入口，`Ipc/` 负责前端
endpoint，`Sidecar/` 负责 MCP server 进程生命周期。

当前已经落地的行为包括：

- `Config.Lua` 提供本机 Agent 类型、CLI 入口和工作目录设置。
- 前端插件和后端插件只在初始化时读取这些设置；设置更新回调只提示重启，不重建运行时。
- CLI 入口留空时，前端按所选 Agent 类型映射默认命令。
- 相对工作目录会解析到相枢 Mod 目录下，并由插件创建；默认包内预置 `AgentWorkspace/AGENTS.md`。
- IPC endpoint manifest 写入 `AgentWorkingDirectory/XiangshuRuntime/ipc-endpoints.json`，不写入用户级
  AppData 目录。manifest 只承担发现职责，不维护额外心跳或可用性状态；endpoint 监听由插件生命周期
  维护，MCP 诊断工具当前用 ping 测试链路。
- 前端把相枢聊天命令注册到游戏原生地图热键分组，默认 `Ctrl+Backslash`（`Ctrl+\`）。
- 聊天热键只在进入存档后的主界面/地图交互中生效；窗口打开后同一热键可关闭窗口。
- 玩家消息会立即显示在运行时生成的聊天窗口中，并进入前端投递队列。
- 前端把等待期间的玩家消息合并成下一轮，启动或恢复所选 CLI Agent 会话，注册当前相枢 MCP endpoint，
  并把 CLI 最终答复显示为相枢消息。
- 前端把当前会话选择、可见聊天消息、消息计数器和外部 Agent 会话 id 写入
  `XiangshuRuntime/ChatSessions/`；重启后只在适配器一致时恢复当前聊天窗口记录并继续复用外部会话 id。
- CLI 失败时，前端只追加一条 `origin = "session"` 的相枢固定失败消息；原始异常、退出码和标准错误
  通过游戏日志记录。
- 游戏内前后端插件不另建持久化日志文件；结构化上下文通过共享日志库写入太吾游戏日志。MCP server 是
  独立进程，把自己的事件日志写入 `XiangshuRuntime/Diagnostics/McpServer/`。

当前对话窗口仍是最小可测界面。MCP server 暴露工具链诊断、IPC ping 和中间答复工具；会修改游戏状态的
工具留到后续迭代，主对话入口仍归前端投递会话。

## Mod 配置语义

太吾 Mod 用户配置提供这些字段：

- `AgentAdapter`：选择 `Codex CLI` 或 `Claude Code`。
- `AgentCliPath`：本机 Agent CLI 的命令名或可执行文件路径；留空时使用当前 Agent 的默认命令。
- `AgentWorkingDirectory`：本机 Agent 使用的工作目录，默认 `AgentWorkspace`。

默认命令按 `AgentAdapter` 决定：Codex CLI 使用 `codex`，Claude Code 使用 `claude`。因此切换
Agent 类型时不需要维护两套路径字段；只有本机 CLI 不在 PATH 或需要固定绝对路径时，才填写
`AgentCliPath`。

`AgentWorkingDirectory` 使用相对路径时，插件会把它解析到相枢 Mod 目录下并创建目录。因此默认值
`AgentWorkspace` 对应：

```text
<Wanxiang.Xiangshu Mod directory>/AgentWorkspace
```

这些配置属于运行时启动参数，只在插件初始化时生效。游戏内修改配置后，`OnModSettingUpdate` 只提示
重启；当前运行时继续使用本次启动时加载的值，重启游戏后再按新配置重建 IPC endpoint、manifest 路径、
MCP sidecar 和前端 Agent 会话。

## 会话投递模型

玩家消息进入前端投递会话后立即写入游戏内可见对话记录。如果当前 CLI Agent 没有工作，消息会立即进入
一个投递轮次；如果 CLI Agent 正在工作，消息会进入 `pendingMessages`，并在上一轮答复完成后与等待
期间的其他消息合并成下一轮。前端通过 `externalSessionId` 恢复本机 Agent 自己的会话；结构化回合输入
只承载本轮玩家消息和参与者信息。

前端投递会话可以按这些核心字段理解。当前实现只保存最小运行所需的数据：

- `sessionId`：前端投递会话 id，由前端生成的 GUID-N 字符串。
- `adapter`：`codex` 或 `claude`。
- `externalSessionId`：CLI Agent 自己的可恢复会话 id；后续轮次用它恢复同一个本机 Agent 会话。
- `visibleMessages`：游戏内可见对话记录，只服务界面渲染和前端元数据。
- `pendingMessages`：已经显示给玩家、尚未进入投递轮次的玩家消息。

持久化快照保存 `sessionId`、`adapter`、`externalSessionId`、消息计数器和 `visibleMessages`，
不保存 `pendingMessages`。

对话消息使用同一种内部模型，不把状态和错误拆成控制面板、占位气泡或消息级状态。显示层只使用当前太吾
真实姓名与相枢两种身份；内部仍可使用 `assistant` 作为协议角色名，但显示层必须把它映射为“相枢”：

- `role`：`user` 或 `assistant`。
- `content`：消息文本；`role = "assistant"` 时显示为相枢消息，`role = "user"` 时保留玩家原文。
- `origin`：`user`、`agent`、`agent-intermediate` 或 `session`。`agent` 表示 CLI 最终 assistant 输出；
  `agent-intermediate` 表示 Agent 通过 MCP 中间答复工具写入的消息；`session` 表示前端会话写入的少量固定
  说明，例如适配器启动失败；它不用于审阅或重写 Agent 输出。

每个投递轮次对应一次 CLI Agent 调用。当前轮次只携带等待期间累积的玩家消息；assistant 答复和中间答复
按产出顺序追加到可见对话记录。后续如果需要把前端侧故障或状态压缩说明交给 Agent，应使用
独立的前端事实字段描述这些事件；可见对话记录仍保持为界面层数据。

## 对话界面映射

对话界面保持单一主交互，不暴露独立的 Agent 控制台，也不暴露“正在输入”“正在思考”“排队中”等 IM
状态。界面只渲染玩家和相枢已经发送出来的消息：

- 玩家消息：玩家发送后立即追加到对话流。
- 相枢消息：只有在 CLI 最终答复、MCP 中间答复工具或前端固定说明产出文本时才追加。
- `working`：不产生可见消息。
- `queued`：不产生可见状态；玩家后续消息仍按发送顺序追加到对话流。
- `failed`：如果需要让玩家知道失败，前端会话追加一条 `origin = "session"` 的相枢固定文本说明；原始异常、
  退出码和标准错误只进入游戏日志。
- `ended`：不显示会话结束状态；如果玩家继续输入，前端要么恢复内部会话，要么追加一条相枢文本说明这段
  对话暂时接不上。

错误说明以相枢消息进入对话流。Codex/Claude 事件格式、进程退出码和 MCP 注册细节进入日志或诊断入口。

这种固定说明在界面上显示为相枢气泡，但元数据必须保留 `origin = "session"`。如果后续需要让 Agent
了解这类前端侧事件，应通过明确的前端事实字段投递。

## MCP 驱动的中间答复

MCP server 提供一个供 Agent 调用的中间答复工具，用于在最终 `reply` 返回前追加一条玩家可见的相枢文本。
它不是速度承诺，也不替代主对话协议；同一轮的最终答复仍由 CLI 调用结果收束。

当前工具：

```text
xiangshu_send_intermediate_reply(
  content
)
```

参数语义：

- `content`：显示给玩家的短文本。

MCP server 收到工具调用后，通过本机 IPC 通知前端。前端当前聊天会话负责追加对话消息；可见文本由调用
该工具的 Agent 提供。需要交代给 Agent 的前端侧事件由投递模型中的前端事实字段承载；MCP server 不保存
长期对话记录，也不主动启动 Agent 调用。

中间答复工具的典型用途：

- 长时间任务开始前，相枢先说明接下来会做什么。
- 相枢已经发现一个可见中间结果，但最终答复还需要继续等待。
- 工具调用链出现可恢复的临时失败，前端需要先让玩家看到原因。

## CLI 适配器边界

适配器的边界是“把一个轮次交给真实本机 CLI Agent，并返回最终答复”。适配器不拥有 MCP 工具，也不
修改游戏状态。

每个适配器接收同一组调用参数：

- CLI 入口。
- 工作目录。
- 相枢 MCP endpoint。
- 可选的外部会话 id；存在时必须恢复同一个 CLI Agent 会话。
- 当前投递轮次的结构化回合输入。
- 最终回复 JSON Schema。

工作目录同时是 CLI Agent 的工作区根目录；其中的指令、设置和 Agent 技能由 CLI Agent 自行加载。

适配器启动 CLI 进程时，必须把进程工作目录设置为 `AgentWorkingDirectory`。进程工作目录是主工作区
来源；命令行参数只补充各 CLI 额外需要的会话参数。

适配器默认使用各 CLI 的完全信任式非交互参数。`AgentWorkingDirectory` 是本机 Agent 的受信工作区；
权限/信任选择在 CLI 启动参数中完成，聊天 UI 只接收最终 JSON 答复或失败说明。如果 CLI 仍因环境约束
中断，前端会话按 `failed` 映射成相枢文本答复。

下面记录当前实现使用的命令形态。

### Codex

Codex 适配器使用 `--dangerously-bypass-approvals-and-sandbox` 进入完全信任式非交互模式。首次调用时
设置 `WorkingDirectory = AgentWorkingDirectory`，同时传入 `--cd <workingDirectory>`，让 Codex 的项目
根与进程工作目录保持一致：

```text
codex exec --dangerously-bypass-approvals-and-sandbox --skip-git-repo-check --json --output-last-message <file> --output-schema <schema-file> --cd <workingDirectory> -
```

结构化回合输入通过 stdin 传入。前端会话从 Codex JSONL 事件里的 `thread.started.thread_id` 捕获
`externalSessionId`，并优先读取 `--output-last-message` 指定临时文件中的最终答复。

后续调用使用捕获到的 `externalSessionId` 恢复同一个 Codex 会话：

```text
codex exec resume --dangerously-bypass-approvals-and-sandbox --skip-git-repo-check --json --output-last-message <file> --output-schema <schema-file> --config 'mcp_servers.xiangshu.url="http://127.0.0.1:<port>/mcp"' <externalSessionId> -
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

适配器从 stream-json 中捕获 `session_id`，并从 result event 提取最终答复。

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

如果后续需要让 Claude Code 访问主工作区之外的目录，再额外使用 `--add-dir <path>`；它不是主工作区
参数。

## 下一步迭代顺序

后续迭代应优先验证真实游戏内 UI 手感，并把运行时生成的窗口替换或收敛为更贴近太吾原生界面的实现。
随后接入需要读取游戏状态的相枢工具。对话 UI 仍不需要直接理解 Codex/Claude
的 CLI 参数，也不需要直接注册 MCP server；这些能力留在前端适配器和 MCP 工具层。

## 本阶段边界

- 排队消息只进入下一轮，不插入 Agent 当前正在执行的轮次。
- MCP 中间答复工具只向当前本地聊天会话追加相枢消息。
- 会修改游戏状态的 MCP 工具留到后续迭代。
