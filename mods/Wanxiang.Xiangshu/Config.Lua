return {
  Title = "相枢（实验中，谨慎启用）",
  Source = 1,
  FileId = 3747795330,
  Version = "0.1.0",
  GameVersion = "1.0.20",
  Author = "Wanxiang Sanctum",
  Description = [=[相枢是一款实验性对话 Mod。游戏内，它让太吾传人借“低语的陶土药钵”与相枢交谈；运行上，它会把玩家新消息和上一条玩家消息之后已经显示的可见聊天片段交给你本机已安装的第三方 CLI Agent，并把最终答复带回游戏。若工作区允许，Agent 还可通过相枢投递受信脚本，读写游戏运行时数据；这些能力可能改动物品、人物，或当前游戏中的其他状态。

[h2]启用前先知晓[/h2]
当前版本仍在试行，适合已经熟悉本机 Agent 工作流、并愿意自行承担受信脚本风险的玩家。如果你只想订阅普通内容、剧情、数值或便利功能 Mod，不建议启用相枢。对话投递与运行时任务可能较慢，也可能失败。

[h2]你需要准备[/h2]
[list]
[*]你已经在本机使用过 Agent，最好有 Codex CLI、Claude Code、CodeBuddy Code 等 CLI 版本经验。
[*]你知道 Agent 工作目录会影响其言行，入口指令、资料、技能、本地文件和运行数据都可能改变结果。
[*]你能自行确认所选第三方本机 Agent 已安装、可启动，并能进入你指定的工作目录。
[*]仅在信任所选 Agent 与工作区时启用相枢。
[/list]

[h2]如何开口[/h2]
[list]
[*]在太吾的 Mod 设置中选择“本机 Agent”。
[*]在“CLI 入口”中填写对应 Agent 的命令名或可执行文件路径；留空时使用所选 Agent 的默认命令。
[*]确认“工作目录”。默认值是 DefaultAgentWorkspace，相对路径会落在相枢 Mod 目录下；初次使用建议先沿用默认工作区。
[*]重启游戏，让相枢重新读取配置，并启动所需的本机组件。
[*]进入存档，回到可操作的主界面或地图交互状态后，按 Ctrl+\ 打开或关闭相枢对话窗口。
[*]相枢会寄身于随身携带的“低语的陶土药钵”。药钵在囊中，便可交谈；药钵离身，发送入口会显示“离身”，需先取回。
[/list]

[h2]相枢如何借 Agent 回应[/h2]
相枢不是内置模型，也不是把命令行窗口嵌进游戏。前端插件只在游戏内维护对话与投递：它把玩家新消息和上一条玩家消息之后的可见聊天片段整理成输入，再按 Mod 设置中的适配器、CLI 入口和工作目录启动第三方 CLI Agent。第三方 Agent 在自己的会话和工具体系中完成推理，前端再把结果收束成游戏中的“相枢”回复。

[list]
[*]当前适配 Codex CLI、Claude Code 和 CodeBuddy Code。
[*]CLI 调用使用非交互模式；玩家只面对游戏内聊天窗口，不需要直接操作 Agent 命令行。
[*]更长程上下文、推理方式和工具选择由第三方 Agent 自己承担；相枢负责游戏内对话流、上一条玩家消息之后的可见聊天片段投递和结果显示。
[*]默认 Agent 工作区放有相枢口吻、静态资料、初始运行经验，以及通过相枢链路读写游戏运行时数据的工具指引。
[/list]

[h2]本机链路与脚本边界[/h2]
相枢会在本机启动一个工具桥接进程，把第三方 Agent 的工具请求接回游戏运行时。这个桥接进程只负责本机路由，不承载游戏逻辑，也不直接持有游戏状态；真正读写游戏状态的代码会在太吾前端或后端插件进程内执行。

[list]
[*]第三方 Agent 只能通过相枢提供的工具入口触达游戏；工具调用、IPC 和脚本执行都留在本机运行链路内。
[*]脚本入口以完全信任方式运行，可能读取或改变当前游戏状态；目标和影响不清楚时，先让 Agent 查验现状。
[*]玩家始终只面对游戏内对话；更细的链路、适配器和脚本说明见开源仓库中的 README 与维护文档。
[/list]

[h2]使用边界[/h2]
相枢不会提供远程 Agent 服务，也不会替你配置第三方 CLI、账号、模型或网络环境。通过相枢投递的脚本在本机受信边界内运行，并可能影响游戏行为；启用前应确认你信任所选 Agent、工作目录及其中指令。

[h2]开源地址[/h2]
https://github.com/Wanxiang-Sanctum/community-taiwu-mods
]=],
  Cover = "Cover.jpg",
  WorkshopCover = "Cover.jpg",
  DetailImageList = {
    "DetailImage1.png",
    "DetailImage2.png",
  },
  TagList = {
    "Extensions",
  },
  Dependencies = {
    [1] = 3747731025,
  },
  DefaultSettings = {
    {
      SettingType = "Dropdown",
      Key = "AgentAdapter",
      DisplayName = "本机 Agent",
      Description = "选择相枢要借用的本机 Agent。",
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
      Description = "相枢唤起本机 Agent 时使用的 CLI 命令名或可执行文件路径。留空时使用所选 Agent 的默认命令。",
      DefaultValue = "",
    },
    {
      SettingType = "InputField",
      Key = "AgentWorkingDirectory",
      DisplayName = "工作目录",
      Description = "本机 Agent 的工作目录。相对路径会落在相枢 Mod 目录下；默认目录随包提供相枢工作区，初次使用可先沿用。",
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
