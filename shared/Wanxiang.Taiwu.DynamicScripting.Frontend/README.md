# Dynamic Scripting 前端适配模块

`Wanxiang.Taiwu.DynamicScripting.Frontend` 提供前端插件复用的动态脚本宿主适配：

- `FrontendScriptEntryDispatcher`：把 `DynamicScriptEntryThread.MainThread` 映射到 Unity 主线程。
- `FrontendScriptReferencePaths`：按调用方显式开启的 `FrontendScriptReferenceFeatures` 解析前端程序集引用路径；内部使用
  `DynamicScriptAssemblyReferenceResolver` 按完整程序集身份定位 DLL。未知 feature 或已开启 feature 的程序集无法定位时直接失败。

本项目依赖前端游戏引用和 UniTask，因此不放入 `Wanxiang.Taiwu.DynamicScripting` 核心项目。核心项目仍只负责编译、
引用解析、入口调用和通用运行结果；前端宿主如何把某个能力映射到精确运行时程序集路径、以及如何切换主线程归本项目。

当前前端运行时依赖通常由前置 Mod 提供；本项目只把当前插件目录和已启用前端插件目录作为定位已声明 feature 程序集的候选范围，
不把这些目录整体暴露成动态脚本引用面。
