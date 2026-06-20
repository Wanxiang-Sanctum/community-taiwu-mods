# 前端模块结构

`FrontendPlugin.cs` 是太吾前端插件生命周期的组合根。子目录按运行职责划分，命名空间跟随目录名并挂在
`Wanxiang.Xiangshu.Frontend` 下。

- `Agent/`：本机 Agent 配置、适配器选择和默认命令/持久化 key。
- `Agent/Cli/`：CLI 进程启动、适配器命令契约、结果解析，以及 CLI 所需的临时协议文件。
- `Agent/Turn/`：前端会话投递给本机 Agent 的轮次模型和结构化输入构造。
- `Chat/`：前端投递会话状态、会话快照持久化、可见聊天消息/事件模型，以及挂载在游戏 UI 层的聊天窗口；
  它维护单一对话入口，并提供玩家视图捕获时排除自身的边界。
- `HotKeys/`：游戏热键注册、前端热键驱动，以及打开或关闭聊天界面所需的 UI 焦点判断。
- `Ipc/`：暴露给本机 MCP server 的前端 MessagePipe endpoint；前端侧脚本执行能力也放在这个边界。
  `Ipc/ItemGrafts/` 只承接后端寄身通知，并转交 `ItemGrafts/` 运行态。
- `ItemGrafts/`：行囊物品寄身风味。它使用 `shared/Wanxiang.Taiwu.ItemGrafts` 创建或附着真实宿主物品，维护本次前端
  运行内的当前 `Graft`、宿主 key 和宿主是否在太吾行囊中，并通过 UI 补丁替换寄身物的外观和太吾行囊内操作菜单。
- `Mcp/`：运行期 MCP bearer token 的生成和 header 表达；供 `Sidecar/` 与 `Agent/Cli/` 注入子进程，不持久化，
  也不进入 endpoint manifest。
- `PlayerView/`：玩家可见前端视图的观察边界；截图由这里捕获，聊天窗口排除由 `Chat/` 提供。
- `Settings/`：相枢本地设置文件读取，负责前端初始化时加载 `LocalSettings.json`。
- `Sidecar/`：MCP server 进程生命周期，并把 sidecar 事件日志定向到
  `.xiangshu-runtime/Diagnostics/McpServer/`。

`FrontendPlugin.cs` 负责组合前端生命周期；子目录之间只在稳定职责边界上协作。`Chat/` 维护前端会话和
待投递消息，用 `Agent/Turn/` 的模型组织投递轮次，并把调用交给 `Agent/Cli/`。`Agent/Cli/` 只负责把该
投递轮次转换为 CLI 调用。`PlayerView/` 捕获玩家视图，需要排除聊天窗口时只依赖 `Chat/` 提供的捕获边界，
不直接处理聊天窗口 UI 结构。新增前端能力时优先放入既有职责目录；出现新的运行职责时再新增同级目录。

`ItemGrafts/` 管理本次前端运行内的 `Graft`、宿主 key 和宿主是否位于太吾行囊。离开 InGame、释放插件或重新读档后，
它清空运行态并让旧的异步物品查询结果失效。重新进入存档时，它以后端 `TaiwuInventorySnapshotChangedRequest` 作为
太吾行囊快照可读的触发点，再按当前行囊重新附着陶土药钵或创建新的真实宿主。
寄身运行态由明确事件唤醒：太吾行囊快照可读信号来自后端太吾行囊观察，当前宿主的物品数据变化来自 `GameDataBridge`
物品监听；前端把当前宿主 key 登记给后端后，后端只报告该宿主的角色行囊转移和物品实例删除。前端收到携带宿主 key
的通知后先按当前宿主复核，再更新聊天可用性或进入重新寄身路径。
当前宿主不在太吾行囊时，已打开的聊天窗口仍可关闭且呈现为不可发送；热键不会从隐藏状态主动唤起聊天窗口。宿主物品实例
本身不再存在时，才进入重新寻找或创建宿主的分支。寄身物菜单调用组合根暴露的聊天窗口入口，并在打开前复核当前宿主仍
在太吾行囊。

本机 Agent 配置读取入口是 `Agent/AgentSettings.cs`，但调用时机由 `FrontendPlugin.cs` 控制：初始化时
读取一次并注入运行时对象。工作目录、CLI 适配器、IPC manifest、MCP sidecar 和 MCP bearer token 由前端
运行时启动流程统一重建；设置修改后由游戏重启生效。

前端侧脚本运行需要本侧插件部署目录。`FrontendPlugin.cs` 从相枢 Mod 目录派生 `Plugins/Frontend` 并
注入 `Ipc/` endpoint；脚本编译和程序集解析规则仍归 `src/Scripting/`。

前端日志调用直接使用 `shared/Wanxiang.Taiwu.Logging`。这个 shared 项目是前后端插件共同的日志适配层；
事件选择和字段取舍归 `docs/logging.md`。行囊物品嫁接协议来自 `shared/Wanxiang.Taiwu.ItemGrafts`；相枢前端负责把
返回的 `Graft` 保存在自己的运行态中，并决定如何在已适配的 UI 入口应用它。
