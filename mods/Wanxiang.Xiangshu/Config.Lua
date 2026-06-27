return {
  Title = "相枢 Mod：在游戏中驱动本地 AI Agent",
  Source = 1,
  FileId = 3747795330,
  Version = "0.1.0",
  GameVersion = "1.0.36",
  Author = "Wanxiang Sanctum",
  Description = [=[[h1]相枢 Mod：在游戏中驱动本地 AI Agent[/h1]
[h2]玩法入口[/h2]
太吾行囊中，会多出一只“低语的陶土药钵”。它自称相枢。打开相枢窗口后，你可以输入问题，或写下想让它判断、查验、处理的目标。

卡在事件选项前，可以问它该看哪条线索；看不懂当前界面，可以让它先看清你眼前有什么；想追一个人的旧事、核对一件物品、弄明白某条机制，也可以把目标交给它。药钵离身时，发送入口会显示“离身”，本轮回应也会中断。

[hr][/hr]

[h2]把问题交给本地 Agent[/h2]
相枢 Mod 会把游戏内的问题或目标交给你本地安装的 Codex CLI、Claude Code 或 CodeBuddy Code，由这些 Agent 完成推理和回复。

Agent 工作区提供相枢的口吻、资料和工具指引。这个 Mod 的工具链负责把 Agent 接回当前游戏：观察画面、读取运行状态、查询机制资料，并在受信边界内执行脚本。

[h2]使用方式[/h2]
[list]
[*]在太吾 Mod 设置中选择你本地安装的 Agent；需要指定命令或路径时，在“CLI 入口”填写命令名或可执行文件路径。
[*]确认“Agent 工作区”。默认值是 DefaultAgentWorkspace；也可以改成你自己维护的目录。
[*]应用设置后重启游戏；进入存档后，在可响应游戏快捷键的界面按 Ctrl+\ 打开相枢窗口，输入内容并点击“传念”发送。
[/list]

[h2]可选：环境变量[/h2]
如果你的本地 Agent 需要代理等环境变量，在相枢 Mod 目录下创建与 Config.Lua 同级的 LocalSettings.json：
[code]{
  "agent": {
    "env": {
      "HTTPS_PROXY": "http://127.0.0.1:7890"
    }
  }
}[/code]
这些变量只写入本地 Agent 子进程环境，不传给游戏进程，也不会写入诊断日志。修改后同样需要重启游戏。

[h2]使用门槛与边界[/h2]
[list]
[*]需要有本地 AI Agent 使用经验；没有可用的本地 Agent，这个 Mod 无法提供服务。
[*]这个 Mod 不设置远程服务器；游戏内会话只写入本地运行数据，所选本地 Agent 也可能按自己的机制保留会话或记录。
[*]不要使用不信任的 Agent 工作区；受信脚本可能读取或改变当前游戏状态。
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
    "DetailImage3.png",
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
      Description = "选择这个 Mod 要调用的本地 Agent。",
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
      Description = "本地 Agent 使用的 Agent 工作区目录。相对路径会落在相枢 Mod 目录下；默认目录随包提供初始 Agent 工作区，初次使用可先沿用。",
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
