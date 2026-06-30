# Dynamic Scripting 前端适配模块

`Wanxiang.Taiwu.DynamicScripting.Frontend` 提供前端插件复用的动态脚本宿主适配：

- `FrontendScriptEntryDispatcher`：把 `DynamicScriptEntryThread.MainThread` 映射到 Unity 主线程。

本项目依赖前端游戏引用和 UniTask，因此不放入 `Wanxiang.Taiwu.DynamicScripting` 核心项目。这里的 UniTask 只服务
`FrontendScriptEntryDispatcher` 对 Unity 主线程的分派实现，不代表动态脚本默认暴露 UniTask 编译引用。

动态脚本引用路径由具体前端宿主自己通过 `DynamicScriptReferenceOptions` 声明。脚本契约、facade 和类似 UniTask
这类由发布依赖提供的运行时能力，分别归对应 Mod 的脚本契约和部署边界；本适配模块不维护这些能力清单，也不扫描插件目录。
