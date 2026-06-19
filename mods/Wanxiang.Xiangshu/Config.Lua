return {
  Title = "相枢（实验阶段，普通用户谨慎订阅）",
  Source = 0,
  FileId = 0,
  Version = "0.1.0",
  GameVersion = "1.0.1",
  Author = "Wanxiang Sanctum",
  Description = [=[相枢是在太吾绘卷内接入第三方本机 CLI Agent 的对话 Mod。它不只是聊天入口：Agent 可以通过相枢投递脚本读写游戏运行时数据，理论上能触达游戏本身具备的能力，例如创建物品、影响 NPC 或改变当前运行状态。

[h2]当前状态[/h2]
当前版本仍处于实验阶段。相枢面向已经熟悉本机 Agent 工作流的玩家；如果你只想订阅普通内容、剧情、数值或便利功能 Mod，建议暂时不要使用。当前对话和运行时任务可能较慢，也有概率失败。

[h2]使用门槛[/h2]
[list]
[*]你已经在本机使用过 Agent，最好有 Codex CLI、Claude Code、CodeBuddy Code 等 CLI 版本的经验。
[*]你了解 Agent 工作目录，知道入口指令、资料、技能和运行数据会影响 Agent 行为。
[*]你能自行确认所选第三方本机 Agent 已安装、可启动，并能使用你指定的工作目录。
[/list]

[h2]使用步骤[/h2]
[list]
[*]在太吾的 Mod 设置中选择“本机 Agent”。
[*]在“CLI 入口”中填写对应 Agent 的命令名或可执行文件路径；留空时按所选 Agent 使用默认命令。
[*]确认“工作目录”。默认值是 DefaultAgentWorkspace，相对路径会放在相枢 Mod 目录下；建议先复用默认工作区。
[*]重启游戏，让相枢重新读取 Agent 配置并启动所需的本机组件。
[*]进入存档，回到能控制主角的主界面或地图交互状态后，按 Ctrl+\ 打开或关闭相枢对话窗口。
[/list]

[h2]它如何与 Agent 交互[/h2]
相枢不是内置模型，也不是把命令行窗口嵌进游戏。前端插件是游戏内对话和第三方 Agent 之间的委托层：它维护聊天窗口和投递状态，把玩家这一轮可见对话整理成任务输入，再按 Mod 设置中的适配器、CLI 入口和工作目录启动第三方 CLI Agent。第三方 Agent 在自己的工作目录、会话和工具体系中完成推理，前端再把结果收束成游戏内的“相枢”回复。

[list]
[*]当前适配 Codex CLI、Claude Code 和 CodeBuddy Code。
[*]CLI 调用使用非交互模式；玩家只面对游戏内聊天窗口，不直接操作 Agent 命令行。
[*]长期上下文、推理方式和工具选择由第三方 Agent 自己承担；相枢负责游戏内对话流、投递和结果显示。
[*]默认 Agent 工作区承载相枢人设、部分仍较粗糙的本地经验，以及让 Agent 通过相枢链路读写游戏运行时数据的工具指引；入口指令、资料、技能、本地文件和运行数据都会影响 Agent 行为。
[/list]

[h2]MCP、IPC 与插件边界[/h2]
相枢会额外启动一个本机 MCP server 进程，把第三方 Agent 的工具调用接回游戏运行时。MCP server 是桥接层：它不承载游戏逻辑，不解释脚本，也不直接持有游戏状态；它接收 Agent 的工具调用，再通过本机 IPC 把需要落到游戏进程的请求转给前端或后端插件。

[list]
[*]前端插件承接游戏内对话层和前端侧脚本入口：负责把玩家可见对话接入 Agent 投递，并在前端插件进程内解释执行脚本投递、返回结果。
[*]后端插件承接后端侧脚本入口：接收 MCP server 转发来的脚本投递，在后端插件进程内解释执行，并通过原链路把执行结果作为 MCP 工具结果返回。
[*]MCP server 负责 Agent 侧工具入口和请求路由；脚本入口的实际执行归前端或后端插件进程。
[*]玩家始终只面对游戏内对话；工具调用、IPC 和脚本执行留在本机运行链路内。
[/list]

[h2]使用边界[/h2]
相枢不会提供远程 Agent 服务，也不会替你配置第三方 CLI、账号、模型或网络环境。通过相枢投递的脚本在本机受信边界内运行，并可能影响游戏行为；请只把它交给你信任的 Agent 和工作目录使用。

[h2]开源地址[/h2]
https://github.com/Wanxiang-Sanctum/community-taiwu-mods
]=],
  TagList = {
    "Extensions",
  },
  Dependencies = {
    3747731025,
  },
  DefaultSettings = {
    {
      SettingType = "Dropdown",
      Key = "AgentAdapter",
      DisplayName = "本机 Agent",
      Description = "选择相枢对话使用的本机 Agent。",
      Options = {
        "Codex CLI",
        "Claude Code",
        "CodeBuddy Code",
      },
      DefaultValue = 0,
    },
    {
      SettingType = "InputField",
      Key = "AgentCliPath",
      DisplayName = "CLI 入口",
      Description = "相枢启动本机 Agent 时使用的 CLI 命令名或可执行文件路径。留空时按所选 Agent 使用默认命令。",
      DefaultValue = "",
    },
    {
      SettingType = "InputField",
      Key = "AgentWorkingDirectory",
      DisplayName = "工作目录",
      Description = "本机 Agent 的工作目录。相对路径会放在相枢 Mod 目录下；默认目录随包提供示例工作区。",
      DefaultValue = "DefaultAgentWorkspace",
    },
  },
  NeedRestartWhenSettingChanged = true,
  BackendPlugins = {
    "Backend/Wanxiang.Xiangshu.Backend.dll",
  },
  FrontendPlugins = {
    "Frontend/Wanxiang.Xiangshu.Frontend.dll",
  },
}
