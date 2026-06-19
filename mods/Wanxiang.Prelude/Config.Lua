return {
  Title = "万象引",
  Source = 0,
  FileId = 0,
  Version = "0.1.0",
  GameVersion = "1.0.1",
  Author = "Wanxiang Sanctum",
  Description = "万象系 Mod 的前置引导层，提供共享运行时和插件依赖解析桥接。需要这些能力的 Mod 应依赖万象引。",
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
