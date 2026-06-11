# 前端模块结构

`FrontendPlugin.cs` 是太吾前端插件生命周期的组合根。子目录按运行职责划分，命名空间跟随目录名并挂在
`Wanxiang.Xiangshu.Frontend` 下。

- `Agent/`：本机 Agent 设置、CLI 进程调用、结构化回合输入构造、结果解析，以及 CLI 所需的临时协议文件。
- `Chat/`：前端投递会话状态、可见聊天消息/事件模型，以及当前运行时生成的聊天窗口。
- `HotKeys/`：游戏热键注册、Harmony 桥接和打开聊天界面所需的 UI 焦点判断。
- `Ipc/`：暴露给本机 MCP server 的前端 MessagePipe endpoint。
- `Sidecar/`：MCP server 进程生命周期，并把独立进程日志定向到 `XiangshuRuntime/Diagnostics/McpServer/`。

依赖方向从 `FrontendPlugin.cs` 指向各子模块。`Chat/` 可以调用 `Agent/` 投递一个对话批次；`Agent/`
只接收自己的 turn DTO，不依赖聊天 UI 或会话模型。新增前端能力时优先放入既有职责目录；只有出现新的
运行职责时才新增同级目录。

本机 Agent 配置读取入口是 `Agent/AgentSettings.cs`，但调用时机由 `FrontendPlugin.cs` 控制：初始化时
读取一次并注入运行时对象。其它子模块不直接读取 Mod 设置，也不在 `OnModSettingUpdate` 路径中热切换
工作目录、CLI 适配器、IPC manifest 或 MCP sidecar。设置修改后由游戏重启重建前端运行时。

前端日志调用直接使用 `shared/Wanxiang.Taiwu.Logging`。这个 shared 项目是前后端插件共同的日志适配层；
前端项目内不新增本地日志包装层。
