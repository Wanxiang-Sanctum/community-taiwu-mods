# 本机 CLI Agent 适配器

本文维护相枢前端可启动的本机 CLI Agent 适配器清单和命令契约。`agent-chat.md` 只描述游戏内对话链路的稳定模型；
新增或调整具体 Agent 时，优先更新本文、`Config.Lua` 和 `src/Frontend/Agent/Cli/`。

## 适配器共同契约

适配器的边界是“把一个投递轮次交给真实本机 CLI Agent，并返回最终答复”。MCP 工具归 MCP server，游戏状态
修改归前端或后端脚本能力。

每个适配器接收同一组调用参数：

- CLI 入口。
- 工作目录。
- 相枢 MCP endpoint。
- 本次运行的 MCP bearer token。
- 可选的外部会话 id；存在时必须恢复同一个 CLI Agent 会话。
- 当前轮次输入 JSON。
- 最终答复 JSON Schema。

工作目录同时是 CLI Agent 的工作区根目录；其中的入口指令、设置和 Agent 技能由 CLI Agent 自行加载。
MCP 工具是否可用、参数和副作用由当前 CLI 调用中暴露的工具说明决定。

适配器启动 CLI 进程时，必须把进程工作目录设置为 `AgentWorkingDirectory`。进程工作目录是主工作区来源；
命令行参数只补充各 CLI 额外需要的会话参数、MCP 配置和结构化输出约束。

首轮 CLI 调用必须返回可恢复会话 id。缺少会话 id 时，前端把它视为 CLI 协议失败，并把当前前端投递会话置为
必须重置状态；玩家只能重置，不能继续在这个本地会话里投递。

最终答复提取是适配器协议的一部分，不是错误兜底。适配器先读取对应 CLI 的主结构化来源；在 CLI 正常结束且
没有返回明确错误结果时，如果主结构化来源缺少 `reply`，适配器最多再从该 CLI 已有的最终消息文本中按同一个
`{ "reply": "..." }` 文档解析一次。仍无法解析时，前端追加协议内固定说明，保持本地投递会话继续可用。

非零退出码和 CLI 明确错误结果仍是失败边界；首轮缺少可恢复会话 id 是必须重置的会话边界。自然语言散文、
stderr 和其它输出形态不作为成功答复的回收来源。

## 当前适配器

表中的官方名称使用对应产品文档或本机 help 暴露的名称；`AgentAdapter` 是相枢内部枚举，默认命令是适配器启动 CLI
进程时使用的命令名。

| `AgentAdapter` | 官方名称 | 默认命令 | 工作区入口 | 会话 id 来源 | 最终答复来源 |
| --- | --- | --- | --- | --- | --- |
| `Codex` | Codex CLI | `codex` | `AGENTS.md`、`.agents/skills/` | JSONL `type = "thread.started"` 事件的 `thread_id` | `--output-last-message` 文件中的 `{ "reply": "..." }` |
| `Claude` | Claude Code | `claude` | `CLAUDE.md`、`.claude/skills/` | stream-json `type = "system"`、`subtype = "init"` 事件的 `session_id` | result event 中的 `structured_output.reply`；缺失时只按同一 schema 解析 `result` 文本 |
| `CodeBuddy` | CodeBuddy Code | `codebuddy` | `AGENTS.md`、`.agents/skills/` | stream-json `type = "system"`、`subtype = "init"` 事件的 `session_id` | result event 中的 `structured_output.reply`；缺失时只按同一 schema 解析 `result` 文本 |

默认工作区不为 CodeBuddy Code 维护单独入口副本；当前适配复用 `AGENTS.md` 和 `.agents/skills/`。

## Codex CLI

Codex 适配器使用 `--dangerously-bypass-approvals-and-sandbox` 进入完全信任式非交互模式。首次调用时设置
`WorkingDirectory = AgentWorkingDirectory`，同时传入 `--cd <workingDirectory>`，让 Codex 的项目根与
进程工作目录保持一致：

```text
codex exec --dangerously-bypass-approvals-and-sandbox --skip-git-repo-check --json --output-last-message <file> --output-schema <schema-file> --cd <workingDirectory> -
```

当前轮次输入 JSON 通过 stdin 传入。前端投递会话从 Codex JSONL 的 `type = "thread.started"` 事件捕获 `thread_id`
作为 `agentSessionId`，并从 `--output-last-message` 指定临时文件提取 `{ "reply": "..." }`。

后续调用使用捕获到的 `agentSessionId` 恢复同一个 Codex 会话：

```text
codex exec resume --dangerously-bypass-approvals-and-sandbox --skip-git-repo-check --json --output-last-message <file> --output-schema <schema-file> --config 'mcp_servers.xiangshu.url="http://127.0.0.1:<port>/mcp"' --config 'mcp_servers.xiangshu.bearer_token_env_var="WANXIANG_XIANGSHU_MCP_BEARER_TOKEN"' <agentSessionId> -
```

注册相枢 MCP endpoint 时，适配器通过 `--config` 临时传入 URL 和 bearer token 所在环境变量；token 值写入
CLI 子进程环境，不写入命令行：

```text
mcp_servers.xiangshu.url="http://127.0.0.1:<port>/mcp"
mcp_servers.xiangshu.bearer_token_env_var="WANXIANG_XIANGSHU_MCP_BEARER_TOKEN"
```

## Print mode + stream-json 适配器

Claude Code 与 CodeBuddy Code 适配器使用 print mode，并通过 `--dangerously-skip-permissions` 进入完全信任式
非交互模式。两者的主工作区来自进程工作目录，因此启动进程时设置 `WorkingDirectory = AgentWorkingDirectory`：

```text
claude --print --output-format stream-json --verbose --dangerously-skip-permissions --mcp-config <file> --json-schema <schema-json>
codebuddy --print --output-format stream-json --verbose --dangerously-skip-permissions --mcp-config <file> --json-schema <schema-json>
```

适配器从 `type = "system"`、`subtype = "init"` 的 stream-json 事件中捕获 `session_id`，并只从
`type = "result"` 且 `is_error = false` 的事件提取最终答复。Windows batch `.CMD` 包装器通过 `%*` 转发参数时
会截断多行 prompt，因此 Claude Code 与 CodeBuddy Code 适配器都把当前轮次输入 JSON 写入 stdin，不把
`<turn-input-json>` 放在命令行末尾。
`--json-schema` 是 Claude Code 与 CodeBuddy Code 的 CLI 参数契约，因此适配器在该边界传递由同一 schema 文档
序列化出的 compact JSON。Claude Code 与 CodeBuddy Code 适配器的最终答复主来源是 result event 的
`structured_output.reply`；如果该字段缺失，且 result event 不是错误结果，适配器只把 result event 的
`result` 文本作为同一 schema 文档再解析一次。

后续调用使用捕获到的 `session_id` 恢复同一个 Claude Code 或 CodeBuddy Code 会话：

```text
claude --print --resume <session_id> --output-format stream-json --verbose --dangerously-skip-permissions --mcp-config <file> --json-schema <schema-json>
codebuddy --print --resume <session_id> --output-format stream-json --verbose --dangerously-skip-permissions --mcp-config <file> --json-schema <schema-json>
```

注册相枢 MCP endpoint 时，适配器写入临时 `--mcp-config` JSON：

```json
{
  "mcpServers": {
    "xiangshu": {
      "type": "http",
      "url": "http://127.0.0.1:<port>/mcp",
      "headers": {
        "Authorization": "Bearer <mcp-bearer-token>"
      }
    }
  }
}
```

如果需要让 Claude Code 或 CodeBuddy Code 访问主工作区之外的目录，再额外使用 `--add-dir <path>`；主工作区仍由
进程工作目录提供。

## 增加适配器

新增适配器时，先确认它是否能复用现有命令族。能复用时扩展对应抽象；不能复用时新增适配器类并把 CLI
协议边界写入本文。更新范围通常包括：

- `Config.Lua` 的 `AgentAdapter` 选项。
- `AgentAdapter` 枚举、`AgentAdapterNames` 默认命令和持久化 adapter key。
- `Agent/Cli/AgentCliAdapters` 映射和具体 `IAgentCliAdapter` 实现。
- 本文的当前适配器表和对应命令契约。

只有新增 Agent 改变了对话投递模型、运行数据所有权、默认工作区资产结构或用户配置语义时，才同步修改
`agent-chat.md`、`agent-context-sources.md` 或相枢 `README.md` 的相关边界说明。
