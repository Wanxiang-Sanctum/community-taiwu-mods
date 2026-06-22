return {
  Title = "万象引（前置依赖，请勿单独订阅）",
  Source = 1,
  FileId = 3747731025,
  Version = "0.1.0",
  GameVersion = "1.0.17",
  Author = "Wanxiang Sanctum",
  Description = [=[如果你不清楚万象引的作用，请不要主动订阅，也不要为了“保险”单独订阅。它不是可游玩的内容 Mod，不会单独新增玩法、剧情或游戏内功能；只有你订阅的其他 Mod 明确要求它，或 Steam 自动提示需要依赖时，才需要订阅。

[h2]它是什么[/h2]
万象引是给其他 Mod 共用的基础组件。它把常见、稳定的运行时和工具栈集中放在一个前置 Mod 中，降低重复携带基础库带来的版本和加载问题。

[h2]前端前置依赖[/h2]
[list]
[*]异步任务与 Unity 前端适配：UniTask，以及 LINQ、TextMeshPro、Addressables 等配套支持。
[*]消息通信与跨进程通信：MessagePipe 及其 Interprocess 支持。
[*]数据序列化：MessagePack。
[*]依赖容器：VContainer，以及 MessagePipe 的 VContainer 集成。
[*]动态脚本支撑：Roslyn C# 编译组件，以及它所需的基础 .NET 支撑库。
[/list]

[h2]后端前置依赖[/h2]
[list]
[*]消息通信与跨进程通信：MessagePipe 及其 Interprocess 支持。
[*]数据序列化：MessagePack。
[*]动态脚本支撑：Roslyn C# 编译组件。
[*]依赖注入：Microsoft.Extensions.DependencyInjection。
[*]后端通信和流处理所需的 .NET 管道（Pipelines）支撑。
[/list]

[h2]加载辅助[/h2]
[list]
[*]万象引会帮助依赖方 Mod 更可靠地分别从前端和后端的 Plugins 子目录解析 DLL，减少同一个 Mod 内前端和后端同名依赖相互干扰。
[/list]

[h2]什么时候需要[/h2]
[list]
[*]你订阅的其他 Mod 要求万象引作为前置依赖。
[*]Steam Workshop 在订阅其他 Mod 时自动提示需要万象引。
[/list]

[h2]开源地址[/h2]
https://github.com/Wanxiang-Sanctum/community-taiwu-mods
]=],
  Cover = "Cover.jpg",
  WorkshopCover = "Cover.jpg",
  TagList = {
    "Frameworks",
  },
  BackendPlugins = {
    "Backend/Wanxiang.Prelude.Backend.dll",
  },
  FrontendPlugins = {
    "Frontend/Wanxiang.Prelude.Frontend.dll",
  },
}
