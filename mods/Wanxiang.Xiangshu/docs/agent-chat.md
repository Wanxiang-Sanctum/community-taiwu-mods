# 相枢对话与本机 Agent 内部设计

## 目标形态

相枢的对话入口由前端插件负责。玩家看到的是与 NPC “相枢”的连续对话，不是 Agent 控制台或通用 AI
产品界面。前端内部维护本机 Agent 会话，并通过适配器调用玩家已经安装并登录过的 CLI Agent；这些都是
实现细节，不进入玩家交互。交互以流畅度优先：前端启动 CLI Agent 时使用无人值守模式，不在对话过程中
向玩家插入权限确认步骤。当前只规划两个适配对象：

- Codex CLI
- Claude Code

MCP server 不承载主对话协议，也不提供“把玩家消息发送给 Agent”的入口。它的职责是作为相枢暴露给
本机 Agent 的工具服务：前端会话在启动 CLI Agent 时，把当前相枢 MCP endpoint 注册给 Agent；Agent
需要读取或操作相枢能力时，再通过 MCP 调用相枢工具。

为了让长时间任务更流畅，MCP server 后续可以额外提供 Agent 快速答复工具。Agent 调用这个工具时，
MCP server 不直接接管会话，而是在内部处理后通过 IPC 通知前端追加、替换或修正对话消息。快速答复
只是让相枢更早发出一条文本，最终答复仍由 CLI 调用结果收束。

```text
前端对话界面
  -> 前端 Agent 会话
    -> Codex CLI / Claude Code
      -> 已注册的相枢 MCP server
        -> 前端/后端插件 IPC 工具
        -> 前端对话消息 IPC 通知
```

## 玩家体验边界

玩家可见层始终是“玩家与相枢对话”。前端可以在内部维护 `session`、`turn`、`batchId`、`origin` 等
协议字段，但界面不展示这些词，也不展示 Codex、Claude、MCP、CLI、权限、进程、stderr、工作目录等实现
细节。

可见交互遵循这些规则：

- 对话对象固定显示为“相枢”，不显示“Agent”“assistant”“模型”“会话”等称呼。
- 对话窗口只有双方发送的消息；没有“思考中”“输入中”“排队中”等独立可见状态。
- 开始、恢复、失败、结束等内部状态只决定是否追加一条相枢消息，不以状态控件、占位气泡或进度提示呈现。
- 相枢不应说自己是 Codex、Claude、工具、MCP server 或本地 Agent。
- 玩家输入框不暴露命令行、会话管理、权限确认、工具调用日志等 Agent 控制面板能力。
- 调试细节只进入开发日志或可展开的诊断入口，不进入默认对话体验。

内部投递给 CLI Agent 的 prompt 必须包含相枢身份约束：对玩家说话时始终以“相枢”的身份回应，不描述
本机 Agent、会话实现或工具链；需要解释临时失败时，也只用符合相枢语气的玩家可见说法。这是投递给
Agent 的生成约束，不设计前端二次审阅、自动重写或基于 Agent 输出的兜底改写流程。前端自身产生的
进程错误、退出码和 stderr 只进入开发日志；需要告知玩家时，使用少量预设的相枢文本说明。

## 当前已落地

当前迭代先铺设太吾 Mod 用户配置、MCP sidecar 和诊断入口：

- `Config.Lua` 提供本机 Agent 类型、CLI 入口和工作目录设置。
- 前端插件在初始化和 Mod 设置更新时读取这些设置。
- CLI 入口留空时，前端按所选 Agent 类型映射默认命令。
- 相对工作目录会解析到相枢 Mod 目录下，并由前端创建。
- IPC endpoint manifest 写入相枢 Mod 目录下的 `AgentWorkspace/ipc-endpoints.json`，不写入用户级
  AppData 目录。
- 前端把诊断命令注册到游戏原生地图热键分组，默认 `Ctrl+Backslash`（`Ctrl+\`）。
- 诊断热键只在进入存档后的主界面/地图交互中生效。它会启动所选 CLI Agent，注册当前相枢 MCP
  endpoint，并要求 Agent 调用 `xiangshu_check_toolchain`。

这些设置目前还没有接入可用对话窗口。MCP server 仍只暴露用于工具链诊断和 IPC ping 的工具，
不提供对话工具，也不修改游戏状态。

## Mod 配置语义

太吾 Mod 用户配置提供这些字段：

- `AgentAdapter`：选择 `Codex CLI` 或 `Claude Code`。
- `AgentCliPath`：本机 Agent CLI 的命令名或可执行文件路径；留空时使用当前 Agent 的默认命令。
- `AgentWorkingDirectory`：本机 Agent 使用的工作目录，默认 `AgentWorkspace`。

默认命令按 `AgentAdapter` 决定：Codex CLI 使用 `codex`，Claude Code 使用 `claude`。因此切换
Agent 类型时不需要维护两套路径字段；只有本机 CLI 不在 PATH 或需要固定绝对路径时，才填写
`AgentCliPath`。

`AgentWorkingDirectory` 使用相对路径时，前端会把它解析到相枢 Mod 目录下并创建目录。因此默认值
`AgentWorkspace` 对应：

```text
<Wanxiang.Xiangshu Mod directory>/AgentWorkspace
```

诊断日志不进入默认玩家对话体验，也不改变 Agent 的无人值守权限策略。需要解析 CLI JSON/stdout 的调用
仍应保留结构化捕获；如果同时需要人眼观察，优先查看开发日志或诊断目录。

## 计划中的异步投递协议

玩家消息进入前端内部 Agent 会话后立即写入对话记录。如果当前 Agent 没有工作，消息会立即进入一个投递
批次；如果 Agent 正在工作，消息会进入 `pendingMessages`，并在上一轮答复完成后与等待期间的其他消息
合并成下一批发给同一 Agent 会话。

内部会话快照包含这些核心字段：

- `sessionId`：前端内部会话 id。
- `adapter`：`codex` 或 `claude`。
- `status`：`idle`、`working`、`failed` 或 `ended`。
- `externalSessionId`：CLI Agent 自己的可恢复会话 id。
- `messages`：玩家可见对话记录。
- `pendingMessages`：已经显示给玩家但尚未投递给 Agent 的消息。
- `turns`：前端投递给 Agent 的批次记录。

对话消息使用同一种内部模型，不把状态和错误拆成控制面板、占位气泡或消息级状态。显示层只使用玩家与
相枢两种身份；内部仍可使用 `assistant` 作为协议角色名，但显示层必须把它映射为“相枢”：

- `role`：`user` 或 `assistant`。
- `content`：消息文本；`role = "assistant"` 时必须是相枢口吻，`role = "user"` 时保留玩家原文。
- `origin`：`user`、`agent`、`agent-tool` 或 `session`。`agent` 表示 CLI 最终 assistant 输出；
  `agent-tool` 表示 Agent 通过 MCP 快速答复工具写入的消息；`session` 表示前端会话写入的少量固定
  说明，例如适配器启动失败；它不用于审阅或重写 Agent 输出。
- `batchId`：关联到对应投递批次；尚未投递时为空。
- `deliveryStatus`：只用于内部投递队列，表示玩家消息是否已经投递给 Agent；不在对话界面显示。
- `contextPolicy`：`omit` 或 `include-once`；用于决定这条可见消息是否要在下一次投递时作为上下文
  一并发给 Agent。

玩家消息的 `deliveryStatus` 有两个值：

- `queued`：消息已经进入前端会话，但 Agent 正在处理上一批。
- `sent`：消息已经被放入某个批次并发送给 Agent。

每个 `turn` 对应一次 CLI Agent 调用。`turn.messageIds` 是本批次包含的玩家消息；assistant 答复会
带上同一个 `batchId` 追加到对话记录。

并非所有可见消息都已经进入 CLI Agent 自己的上下文。前端会话写入的固定失败说明、`agent-tool`
快速答复，以及被前端压缩过的状态说明，都要根据 `contextPolicy` 处理：

- `omit`：只用于界面反馈，不进入下一轮 Agent prompt。
- `include-once`：在下一轮投递玩家消息时，作为“前端会话记录”随同发送给 Agent；发送成功后改回
  `omit`，避免重复灌入。

临时失败默认使用 `include-once`。这样玩家下一次追问时，Agent 能知道上一轮并非自己完整答复失败，而是
前端会话或工具调用在某个位置中断。

构造下一轮 prompt 时，`include-once` 消息先组成一个有边界的“前端会话记录”块，再与本轮玩家消息一并
投递。这个块要保留 `origin`、`batchId` 和简短时间顺序说明，不能把 `session` 或 `agent-tool` 消息
伪装成 CLI 历史里的 assistant 消息。

## 对话界面映射

对话界面保持单一主交互，不暴露独立的 Agent 控制台，也不暴露“正在输入”“正在思考”“排队中”等 IM
状态。界面只渲染玩家和相枢已经发送出来的消息：

- 玩家消息：玩家发送后立即追加到对话流。
- 相枢消息：只有在 CLI 最终答复、MCP 快速答复工具或前端固定说明产出文本时才追加。
- `working`：不产生可见消息。
- `queued`：不产生可见状态；玩家后续消息仍按发送顺序追加到对话流。
- `failed`：如果需要让玩家知道失败，前端会话追加一条 `origin = "session"` 的相枢固定文本说明；原始异常、
  退出码和 stderr 只进入开发日志或可展开详情。
- `ended`：不显示会话结束状态；如果玩家继续输入，前端要么恢复内部会话，要么追加一条相枢文本说明这段
  对话暂时接不上。

错误答复应该像相枢给出的文本回答，而不是裸错误弹窗。玩家不需要理解 Codex/Claude 的事件格式、进程
退出码或 MCP 注册细节。

这种固定说明在界面上显示为相枢气泡，但元数据必须保留 `origin = "session"`。如果它会影响玩家下一次
输入的理解，就设置 `contextPolicy = "include-once"`，在下一次投递时把它作为上下文说明给 Agent，而
不是假装它是 Agent 亲自生成的历史消息。

## MCP 驱动的快速答复

MCP server 可以提供一个供 Agent 调用的快速答复工具，用于把短文本尽快推到前端。它解决的是“长时间
任务期间没有相枢回复”的体验问题，不替代主对话协议，也不引入独立状态展示。

工具草案：

```text
xiangshu_post_chat_update(
  sessionId,
  batchId,
  kind,
  content,
  contextPolicy,
  targetMessageId?
)
```

参数语义：

- `sessionId`：前端会话 id，用来避免更新落到错误窗口。
- `batchId`：当前投递批次 id，用来把快速答复关联到正在执行的任务。
- `kind`：`append-message` 或 `replace-message`。两者都只处理对话消息，不处理独立状态。
- `content`：显示给玩家的短文本；必须符合相枢口吻，不传输原始调试日志。
- `contextPolicy`：默认 `omit`；当这条更新会影响后续推理时使用 `include-once`。
- `targetMessageId`：仅 `replace-message` 使用，用来指定要替换的既有相枢消息。

MCP server 收到工具调用后，通过本机 IPC 通知前端。前端负责校验会话和批次、追加或替换对话消息，
以及决定这条消息是否进入下一轮上下文。可见文本是否符合相枢口吻属于 Agent prompt 和工具调用约束，
这里不增加自动审阅或改写流程。MCP server 不直接保存长期对话记录，也不主动启动 Agent 调用。

快速答复工具的典型用途：

- 长时间任务开始前，相枢先说明接下来会做什么。
- 相枢已经发现一个可见中间结果，但最终答复还需要继续等待。
- 工具调用链出现可恢复的临时失败，前端需要先让玩家看到原因，并把这条说明带入下一轮。

## 计划中的 CLI 适配器

适配器的边界是“把一个批次交给真实本机 CLI Agent，并返回最终答复”。适配器不拥有 MCP 工具，也不
修改游戏状态。

每个适配器复用同一组输入：

- CLI 入口。
- 工作目录。
- 相枢 MCP endpoint。
- 可选的外部会话 id。
- 相枢身份约束和玩家可见表达规则。
- 当前投递批次的玩家消息。

适配器启动 CLI 进程时，必须把进程工作目录设置为 `AgentWorkingDirectory`。命令行参数只记录各 CLI
额外需要的会话参数；不能把“未写在命令行里”理解成没有指定工作区。

适配器还必须以无人值守权限模式启动 CLI Agent。相枢不在对话窗口中代理 CLI 权限确认，也不把权限确认
插入下一批玩家消息；如果 CLI 仍然因为权限、信任或环境约束而中断，前端会话按 `failed` 映射成相枢
文本答复。

下面的命令形态是后续实现的适配草案；落地时需要用本机 CLI 实际行为验证参数、事件格式和恢复语义。

### Codex

Codex 适配器使用非交互模式。启动进程时设置 `WorkingDirectory = AgentWorkingDirectory`，同时传入
`--cd <workingDirectory>`，让 Codex 的项目根与进程工作目录保持一致：

```text
codex exec --dangerously-bypass-approvals-and-sandbox --skip-git-repo-check --json --output-last-message <file> --cd <workingDirectory> -
codex exec resume --dangerously-bypass-approvals-and-sandbox --skip-git-repo-check --json --output-last-message <file> <externalSessionId> -
```

prompt 通过 stdin 传入。前端会话从 Codex JSONL 事件里的 `thread.started.thread_id` 捕获
`externalSessionId`，并优先读取 `--output-last-message` 指定文件中的最终答复。

注册相枢 MCP endpoint 时，适配器通过 `--config` 临时传入：

```text
mcp_servers.xiangshu.url="http://127.0.0.1:<port>/mcp"
```

### Claude Code

Claude Code 适配器使用 print mode。Claude Code 的主工作区来自进程工作目录，因此启动进程时设置
`WorkingDirectory = AgentWorkingDirectory`：

```text
claude --print --output-format stream-json --verbose --dangerously-skip-permissions --mcp-config <file> <prompt>
claude --print --output-format stream-json --verbose --dangerously-skip-permissions --mcp-config <file> --resume <externalSessionId> <prompt>
```

适配器从 stream-json 中捕获 `session_id`，并从 result event 或 assistant text content 提取最终
答复。

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

## 后续可测试交互

前端下一步应优先形成可测试的最小对话路径：

- 使用太吾 Mod 设置读取 Agent 类型、CLI 入口和工作目录。
- 创建和结束前端内部 Agent 会话。
- 以无人值守模式启动所选 CLI Agent，并在调用时注册当前相枢 MCP endpoint。
- 将玩家连续输入合并成批次投递；界面只显示玩家和相枢已经发出的消息，不显示排队状态。
- 把需要告知玩家的错误映射成相枢消息；工作状态和排队状态不产生可见 UI。

对话 UI 不需要直接理解 Codex/Claude 的 CLI 参数，也不需要直接注册 MCP server；这些都留在前端适配器
里。

## 本阶段暂不处理

- 不实现可用对话窗口。
- 不把排队消息插入 Agent 当前正在执行的批次。
- 不提供 CLI Agent 的交互式权限确认流程。
- 不持久化 Agent 会话；前端插件卸载后内存会话丢失。
- 不开放会修改游戏状态的 MCP 工具。
