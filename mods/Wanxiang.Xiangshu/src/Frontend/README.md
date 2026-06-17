# 前端模块结构

`FrontendPlugin.cs` 是太吾前端插件生命周期的组合根。子目录按运行职责划分，命名空间跟随目录名并挂在
`Wanxiang.Xiangshu.Frontend` 下。

- `Agent/`：本机 Agent 设置、CLI 进程调用、结构化投递输入构造、结果解析，以及 CLI 所需的临时协议文件。
- `Chat/`：前端投递会话状态、会话快照持久化、可见聊天消息/事件模型，以及挂载在游戏 UI 层的聊天窗口；
  它维护单一对话入口，并提供玩家视图捕获时排除自身的边界。
- `HotKeys/`：游戏热键注册、前端热键驱动，以及打开聊天界面所需的 UI 焦点判断。
- `Ipc/`：暴露给本机 MCP server 的前端 MessagePipe endpoint；前端侧脚本执行能力也放在这个边界。
- `Mcp/`：运行期 MCP bearer token 的生成和 header 表达；供 `Sidecar/` 与 `Agent/` 注入子进程，不持久化，
  也不进入 endpoint manifest。
- `PlayerView/`：玩家可见前端视图的观察边界；截图由这里捕获，聊天窗口排除由 `Chat/` 提供。
- `Settings/`：相枢本地设置文件读取，负责前端初始化时加载 `LocalSettings.json`。
- `Sidecar/`：MCP server 进程生命周期，并把 sidecar 事件日志定向到
  `.xiangshu-runtime/Diagnostics/McpServer/`。

`FrontendPlugin.cs` 负责组合前端生命周期；子目录之间只在稳定职责边界上协作。`Chat/` 维护前端会话和
待投递消息，并把一个投递轮次交给 `Agent/`；`Agent/` 只负责把该投递轮次转换为 CLI 调用。`PlayerView/`
捕获玩家视图，需要排除聊天窗口时只依赖 `Chat/` 提供的捕获边界，不直接处理聊天窗口 UI 结构。新增前端能力
时优先放入既有职责目录；出现新的运行职责时再新增同级目录。

本机 Agent 配置读取入口是 `Agent/AgentSettings.cs`，但调用时机由 `FrontendPlugin.cs` 控制：初始化时
读取一次并注入运行时对象。工作目录、CLI 适配器、IPC manifest、MCP sidecar 和 MCP bearer token 由前端
运行时启动流程统一重建；设置修改后由游戏重启生效。

前端侧脚本运行需要本侧插件部署目录。`FrontendPlugin.cs` 从相枢 Mod 目录派生 `Plugins/Frontend` 并
注入 `Ipc/` endpoint；脚本编译和程序集解析规则仍归 `src/Scripting/`。

前端日志调用直接使用 `shared/Wanxiang.Taiwu.Logging`。这个 shared 项目是前后端插件共同的日志适配层；
事件选择和字段取舍归 `docs/logging.md`。
