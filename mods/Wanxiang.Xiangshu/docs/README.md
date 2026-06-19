# 相枢维护文档

本目录存放相枢源码维护者阅读的内部设计文档。它们不随 mod 组包进入可部署目录，也不作为运行中 Agent 的工作区指令。

## 阅读入口

- `agent-chat.md`：游戏内对话、本机 CLI Agent、MCP sidecar、脚本通道、中间答复和运行数据目录的运行设计。它说明
  默认工作区在对话链路中的运行契约，不维护默认工作区资料来源、放置规则或具体 CLI 命令清单。
- `agent-cli-adapters.md`：本机 CLI Agent 适配器清单、命令形态、工作区入口、会话 id 来源、最终答复来源和新增
  适配器时的更新边界。它不重复游戏内对话、MCP sidecar 或脚本通道的完整运行模型。
- `logging.md`：游戏日志、MCP sidecar 事件日志和可见对话的分工。它不维护共享日志库 API，也不复制运行数据文件格式。
- `agent-context-sources.md`：`DefaultAgentWorkspace/` 资料如何回溯到太吾游戏文本、配置、游戏侧源码和运行时事实，以及
  组织内部游戏快照如何辅助复核；新资料应放到哪里，以及本地工作记录、默认资产和运行数据如何分界。它不替代运行中的
  `AGENTS.md`、`CLAUDE.md`、`persona/`、`lore/` 或 `tool-guides/`。

## 与其它文档的关系

相枢根 `README.md` 面向读者选择入口、开发命令和项目结构；本目录解释较深的运行设计和维护边界。源码子目录下的
README 只说明本模块职责、依赖方向和增长规则，不重复完整对话链路。

源码中的 `DefaultAgentWorkspace/` 是组包后默认本机 Agent 工作区的内容源。其中的 `AGENTS.md`、`CLAUDE.md`、
`persona/`、`lore/`、`tool-guides/`、`.agents/skills/` 和 `.claude/skills/` 由运行中的 Agent 读取；这些文件应
保持自包含，不依赖本目录或开发机上的游戏快照路径。各 CLI Agent 使用哪个入口文件和技能目录由
`agent-cli-adapters.md` 维护。
