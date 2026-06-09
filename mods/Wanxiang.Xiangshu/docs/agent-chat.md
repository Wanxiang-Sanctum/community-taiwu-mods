# 相枢本机 Agent 会话设计

## 目标形态

相枢的聊天入口由前端插件负责，用户只看到简单的聊天交互。前端内部维护本机 Agent 会话，并通过适配器
调用用户已经安装并登录过的 CLI Agent。当前只规划两个适配对象：

- Codex CLI
- Claude Code

MCP server 不承载聊天协议，也不提供“发送消息给 Agent”的工具。它的职责是作为相枢暴露给本机 Agent
的工具服务：前端会话在启动 CLI Agent 时，把当前相枢 MCP endpoint 注册给 Agent；Agent 需要读取或
操作相枢能力时，再通过 MCP 调用相枢工具。

```text
前端聊天界面
  -> 前端 Agent 会话
    -> Codex CLI / Claude Code
      -> 已注册的相枢 MCP server
        -> 前端/后端插件 IPC 工具
```

## 当前已落地

当前迭代先铺设太吾 Mod 用户配置和前端侧会话设置边界：

- `Config.Lua` 提供本机 Agent 类型、CLI 入口和会话工作目录设置。
- 前端插件在初始化和 Mod 设置更新时读取这些设置。
- CLI 入口留空时，前端按所选 Agent 类型映射默认命令。
- 相对工作目录会解析到相枢 Mod 目录下，并由前端创建。

这些设置目前还没有接入完整聊天窗口，也没有实际启动 CLI Agent。MCP server 仍只暴露用于 smoke demo
的 ping 工具，不提供聊天工具，也不修改游戏状态。

## 用户配置语义

太吾 Mod 用户配置提供这些字段：

- `AgentAdapter`：选择 `Codex CLI` 或 `Claude Code`。
- `AgentCliPath`：本机 Agent CLI 的命令名或可执行文件路径；留空时使用当前 Agent 的默认命令。
- `AgentWorkingDirectory`：本机 Agent 会话工作目录，默认 `AgentWorkspace`。

默认命令按 `AgentAdapter` 决定：Codex CLI 使用 `codex`，Claude Code 使用 `claude`。因此用户切换
Agent 类型时不需要维护两套路径字段；只有本机 CLI 不在 PATH 或需要固定绝对路径时，才填写
`AgentCliPath`。

`AgentWorkingDirectory` 使用相对路径时，前端会把它解析到相枢 Mod 目录下并创建目录。因此默认值
`AgentWorkspace` 对应：

```text
<Wanxiang.Xiangshu Mod directory>/AgentWorkspace
```

## 计划中的异步聊天协议

用户消息进入前端 Agent 会话后立即写入聊天记录。如果当前 Agent 没有工作，消息会立即进入一个投递
批次；如果 Agent 正在工作，消息会进入 `pendingMessages`，并在上一轮答复完成后与等待期间的其他消息
合并成下一批发给同一 Agent 会话。

内部会话快照包含这些核心字段：

- `sessionId`：前端内部会话 id。
- `adapter`：`codex` 或 `claude`。
- `status`：`idle`、`working`、`failed` 或 `ended`。
- `externalSessionId`：CLI Agent 自己的可恢复会话 id。
- `messages`：用户可见聊天记录。
- `pendingMessages`：已经显示给用户但尚未投递给 Agent 的消息。
- `turns`：前端投递给 Agent 的批次记录。

用户消息的 `deliveryStatus` 有两个值：

- `queued`：消息已经进入前端会话，但 Agent 正在处理上一批。
- `sent`：消息已经被放入某个批次并发送给 Agent。

每个 `turn` 对应一次 CLI Agent 调用。`turn.messageIds` 是本批次包含的用户消息；assistant 答复会
带上同一个 `batchId` 追加到聊天记录。

## 计划中的 CLI 适配器

适配器的边界是“把一个批次交给真实本机 CLI Agent，并返回最终答复”。适配器不拥有 MCP 工具，也不
修改游戏状态。

每个适配器复用同一组输入：

- CLI 入口。
- 会话工作目录。
- 相枢 MCP endpoint。
- 可选的外部会话 id。
- 当前投递批次的用户消息。

适配器启动 CLI 进程时，必须把进程工作目录设置为 `AgentWorkingDirectory`。命令行参数只记录各 CLI
额外需要的会话参数；不能把“未写在命令行里”理解成没有指定工作区。

下面的命令形态是后续实现的适配草案；落地时需要用本机 CLI 实际行为验证参数、事件格式和恢复语义。

### Codex

Codex 适配器使用非交互模式。启动进程时设置 `WorkingDirectory = AgentWorkingDirectory`，同时传入
`--cd <workingDirectory>`，让 Codex 的项目根与进程工作目录保持一致：

```text
codex exec --json --output-last-message <file> --cd <workingDirectory> -
codex exec resume --json --output-last-message <file> <externalSessionId> -
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
claude --print --output-format stream-json --verbose <prompt>
claude --print --output-format stream-json --verbose --resume <externalSessionId> <prompt>
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

前端下一步应优先形成可测试的最小聊天路径：

- 使用太吾 Mod 设置读取 Agent 类型、CLI 入口和工作目录。
- 创建和结束前端内部 Agent 会话。
- 启动所选 CLI Agent，并在调用时注册当前相枢 MCP endpoint。
- 将用户连续输入合并成批次投递，在界面上区分已发送和排队消息。
- 根据会话状态渲染聊天记录、工作状态和错误。

聊天 UI 不需要直接理解 Codex/Claude 的 CLI 参数，也不需要直接注册 MCP server；这些都留在前端适配器
里。

## 本阶段暂不处理

- 不实现完整聊天窗口。
- 不把排队消息插入 Agent 当前正在执行的批次。
- 不处理 CLI Agent 的交互式权限确认。
- 不持久化 Agent 会话；前端插件卸载后内存会话丢失。
- 不开放会修改游戏状态的 MCP 工具。
