# 后端模块结构

`BackendPlugin.cs` 是太吾后端插件生命周期的组合根。后端职责集中在启动供 MCP sidecar 调用的后端 MessagePipe IPC
endpoint、安装 shared 物品嫁接后端创建与观察服务，并把 endpoint 注册到 `.xiangshu-runtime/ipc-endpoints.json`。

后端侧脚本执行能力归这个模块：它承载后端进程可访问的游戏 API、数据状态和线程边界。

宿主创建与观察由 `shared/Wanxiang.Taiwu.ItemGrafts.Backend` 提供。后端初始化时调用
`BackendItemGrafts.Install(this)`；shared 组件通过 `Wanxiang.Taiwu.ModRpc` 接收前端 session 订阅，并观察宿主
删除、游戏 owner 变化和宿主数据变化。相枢 Mod 后端的边界是安装该 shared 服务；宿主订阅、session 统计和事件转发归 shared
组件。

启动时，后端从游戏 Mod 管理接口取得相枢 Mod 目录，并把 `Plugins/Backend` 作为本侧插件部署目录传给共享
脚本运行器。`entryThread = mainThread` 的入口分派复用 `shared/Wanxiang.Taiwu.DynamicScripting.Backend`，
排入 GameData 主循环。脚本契约 DLL 引用路径来自 `src/Scripting/`；后端只加入该契约引用，不额外开放前端运行时能力。

入口契约、契约 DLL 引用路径和 Mod 侧响应映射归 `src/Scripting/`，通用编译和临时程序集依赖解析归 shared
动态脚本运行核心；后端负责本侧 endpoint 和后端 API 边界。
