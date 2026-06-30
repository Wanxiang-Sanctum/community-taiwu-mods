# Dynamic Scripting 前端适配模块

`Wanxiang.Taiwu.DynamicScripting.Frontend` 提供前端插件复用的动态脚本宿主适配：

- `FrontendScriptEntryDispatcher`：把 `DynamicScriptEntryThread.MainThread` 映射到 Unity 主线程。
- `FrontendScriptReferencePaths`：从当前前端插件目录和已启用 Mod 的前端插件目录中解析常用额外程序集引用路径；无法定位时返回空列表，需要该程序集的脚本会在编译阶段收到 Roslyn 诊断。

本项目依赖前端游戏引用和 UniTask，因此不放入 `Wanxiang.Taiwu.DynamicScripting` 核心项目。核心项目仍只负责编译、
引用解析、入口调用和通用运行结果；前端宿主如何找到运行时程序集和切换主线程归本项目。
