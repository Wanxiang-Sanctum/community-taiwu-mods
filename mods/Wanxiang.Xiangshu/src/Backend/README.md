# 后端模块结构

`BackendPlugin.cs` 是太吾后端插件生命周期的组合根。当前后端职责集中在启动后端 MessagePipe IPC
endpoint，并把 endpoint 注册到 `.xiangshu-runtime/ipc-endpoints.json`。

后端侧脚本执行能力归这个模块：它承载后端进程可访问的游戏 API、数据状态和线程边界。
