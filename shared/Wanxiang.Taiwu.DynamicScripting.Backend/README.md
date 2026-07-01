# Dynamic Scripting 后端适配模块

`Wanxiang.Taiwu.DynamicScripting.Backend` 提供后端插件复用的动态脚本宿主适配：

- `BackendScriptEntryDispatcher`：把 `DynamicScriptEntryThread.MainThread` 映射到 GameData 主循环。

本项目依赖后端游戏引用，因此不放入 `Wanxiang.Taiwu.DynamicScripting` 核心项目。核心项目仍只负责编译、
引用解析、入口调用和通用运行结果；后端宿主如何进入 GameData 主循环归本项目。
