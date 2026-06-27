return {
  Title = "相枢：接入太吾运行时的本地 AI Agent",
  Source = 1,
  FileId = 3747795330,
  Version = "0.1.0",
  GameVersion = "1.0.36",
  Author = "Wanxiang Sanctum",
  Description = [=[[h1]相枢：接入太吾运行时的本地 AI Agent[/h1]
[h2]先看是否适合[/h2]
相枢把本地 CLI Agent 接入当前运行中的太吾绘卷。你在游戏里提出愿望、问题或命令，Agent 会按目标动态执行脚本，尝试完成与游戏内容有关的查找、判断或改动；结果由相枢带回游戏内对话。

目前适配 Codex CLI、Claude Code 和 CodeBuddy Code。

[hr][/hr]

[h2]游戏内入口[/h2]
一只“低语的陶土药钵”会落入太吾行囊。它自称相枢，听你把疑问、欲求与命令递入钵中。

卡在事件选项前，可以让它窥清眼前所见；追查人物旧事、核对物品来历、辨认机制牵连时，也可以把目标交给它。若你要它动手，它会先寻证，再沿可落成的路径下手；成败、代价和余波都会回到这只药钵里。药钵在身时方可传念，离身则声息沉寂，本轮回应也会中断。

[h2]使用方式[/h2]
[list]
[*]在太吾 Mod 设置中选择本地 Agent；必要时填写“CLI 入口”和“Agent 工作区”。
[*]应用设置后重启游戏；进入存档后按 Ctrl+\ 打开相枢窗口，也可从药钵操作菜单进入。
[/list]

[h2]本地数据与信任边界[/h2]
[list]
[*]相枢 Mod 自己不设置远程服务器；游戏内会话写入本地运行数据，所选 Agent 也可能按自己的机制保留记录。
[*]当玩家目标需要行动时，Agent 可借脚本读取或改变当前游戏、界面和 Mod 状态。脚本在插件进程内运行，不提供沙箱。
[*]对话窗口状态不写入游戏存档。
[/list]

[h2]更多说明[/h2]
开源仓库和详细 README：
[url=https://github.com/Wanxiang-Sanctum/community-taiwu-mods]https://github.com/Wanxiang-Sanctum/community-taiwu-mods[/url]
]=],
  Cover = "Cover.png",
  WorkshopCover = "Cover.png",
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
      DisplayName = "本地 Agent",
      Description = "选择相枢要调用的本地 Agent。",
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
      Description = "相枢 Mod 调用本地 Agent 时使用的 CLI 命令名或可执行文件路径。留空时使用所选 Agent 的默认命令。",
      DefaultValue = "",
    },
    {
      SettingType = "InputField",
      Key = "AgentWorkingDirectory",
      DisplayName = "Agent 工作区",
      Description = "本地 Agent 使用的 Agent 工作区目录，负责相枢口吻、资料路线、工具用法和可动手范围。相对路径会落在相枢 Mod 目录下；默认目录随包提供。",
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
