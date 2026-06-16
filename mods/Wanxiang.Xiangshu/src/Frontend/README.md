# 前端模块结构

`FrontendPlugin.cs` 是太吾前端插件生命周期的组合根。子目录按运行职责划分，命名空间跟随目录名并挂在
`Wanxiang.Xiangshu.Frontend` 下。

- `Agent/`：本机 Agent 设置、CLI 进程调用、结构化回合输入构造、结果解析，以及 CLI 所需的临时协议文件。
- `Chat/`：前端投递会话状态、会话快照持久化、可见聊天消息/事件模型，以及游戏内聊天窗口；它维护单一
  对话入口。
- `HotKeys/`：游戏热键注册、Harmony 桥接和打开聊天界面所需的 UI 焦点判断。
- `Ipc/`：暴露给本机 MCP server 的前端 MessagePipe endpoint；前端侧脚本执行能力也放在这个边界。
- `Settings/`：相枢本地设置文件读取，负责前端初始化时加载 `LocalSettings.json`。
- `Sidecar/`：MCP server 进程生命周期，并把独立进程日志定向到 `.xiangshu-runtime/Diagnostics/McpServer/`。

依赖方向从 `FrontendPlugin.cs` 指向各子模块。`Chat/` 维护前端会话和待投递消息，并把一个对话轮次交给
`Agent/`；`Agent/` 只负责把该轮次转换为 CLI 调用。新增前端能力时优先放入既有职责目录；出现新的运行
职责时再新增同级目录。

本机 Agent 配置读取入口是 `Agent/AgentSettings.cs`，但调用时机由 `FrontendPlugin.cs` 控制：初始化时
读取一次并注入运行时对象。工作目录、CLI 适配器、IPC manifest 和 MCP sidecar 由前端运行时启动流程
统一重建；设置修改后由游戏重启生效。

前端侧脚本运行需要本侧插件部署目录。`FrontendPlugin.cs` 从相枢 Mod 目录派生 `Plugins/Frontend` 并
注入 `Ipc/` endpoint；脚本编译和程序集解析规则仍归 `src/Scripting/`。

前端日志调用直接使用 `shared/Wanxiang.Taiwu.Logging`。这个 shared 项目是前后端插件共同的日志适配层。
