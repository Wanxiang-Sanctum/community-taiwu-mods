# 相枢维护文档

本目录存放相枢源码维护者阅读的内部设计文档。它们不随 mod 组包进入可部署目录，也不作为运行中 Agent 的工作区指令。

## 阅读入口

- `agent-chat.md`：游戏内对话、本机 CLI Agent、MCP sidecar、脚本通道、中间答复和运行数据目录的运行设计。它只保留
  默认工作区的运行时契约，不维护默认工作区资料来源或放置规则。
- `logging.md`：游戏日志、MCP sidecar 事件日志和可见对话的分工。它不维护共享日志库 API，也不复制运行数据文件格式。
- `agent-context-sources.md`：`DefaultAgentWorkspace/` 资料如何回溯到游戏观察快照，以及新资料应放到哪里。它不替代
  运行中的 `AGENTS.md`、`persona/`、`lore/` 或 `tool-guides/`。

## 与其它文档的关系

相枢根 `README.md` 面向读者选择入口、开发命令和项目结构；本目录解释较深的运行设计和维护边界。源码子目录下的
README 只说明本模块职责、依赖方向和增长规则，不重复完整对话链路。

源码中的 `DefaultAgentWorkspace/` 是组包后默认本机 Agent 工作区的内容源。其中的 `AGENTS.md`、`persona/`、`lore/`、
`tool-guides/`、`.agents/skills/` 和 `.claude/skills/` 由运行中的 Agent 读取；这些文件应保持自包含，不依赖本目录
或开发机上的游戏快照路径。
