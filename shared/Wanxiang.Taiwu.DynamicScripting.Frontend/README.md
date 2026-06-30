# Dynamic Scripting 前端适配模块

`Wanxiang.Taiwu.DynamicScripting.Frontend` 提供前端插件复用的动态脚本宿主适配：

- `FrontendScriptEntryDispatcher`：把 `DynamicScriptEntryThread.MainThread` 映射到 Unity 主线程。
- `FrontendScriptReferences.CreateOptions`：为前端脚本创建 `DynamicScriptReferenceOptions`。调用方提供当前插件目录、脚本契约 marker
  type 和需要额外开放给脚本的 facade marker type；本模块固定加入 UniTask 编译引用。内部使用
  `DynamicScriptAssemblyReferenceResolver` 按完整程序集身份定位 DLL，契约程序集、facade 程序集或 UniTask 无法定位时直接失败。

本项目依赖前端游戏引用和 UniTask，因此不放入 `Wanxiang.Taiwu.DynamicScripting` 核心项目。核心项目仍只负责编译、
引用解析、入口调用和通用运行结果；前端宿主如何把脚本契约和前端固定能力映射到精确运行时程序集路径、以及如何切换主线程归本项目。

脚本契约和调用方额外开放的 facade 属于当前插件边界；已加载程序集没有可用位置时，只在当前插件目录查找对应 DLL。
UniTask 属于前端固定能力，运行时可能由前置 Mod 提供；已加载 UniTask 没有可用位置时，会在当前插件目录和已启用前端插件目录中定位。
本项目只按完整程序集身份查找这些已声明程序集，不把候选目录整体暴露成动态脚本引用面。
