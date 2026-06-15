# 后端模块结构

`BackendPlugin.cs` 是太吾后端插件生命周期的组合根。后端职责集中在启动后端 MessagePipe IPC
endpoint，并把 endpoint 注册到 `.xiangshu-runtime/ipc-endpoints.json`。

后端侧脚本执行能力归这个模块：它承载后端进程可访问的游戏 API、数据状态和线程边界。

启动时，后端从游戏 Mod 管理接口取得相枢 Mod 目录，并把 `Plugins/Backend` 作为本侧插件部署目录传给共享
脚本运行器。脚本编译和程序集解析规则归 `src/Scripting/`；后端只决定本侧 endpoint 和后端 API 边界。
