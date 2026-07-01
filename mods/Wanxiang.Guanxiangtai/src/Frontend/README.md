# 前端模块结构

`src/Frontend/` 是观象台前端插件入口。它负责这些事：

- 确保包内 MCP server 进程已启动。
- 启动供 MCP server 调用的前端 MessagePipe IPC endpoint，并在 `.guanxiangtai-runtime/ipc-endpoints.json` 中登记
  role 为 `frontend` 的内部入口。
- 承接前端侧状态检测、游戏退出和受信 C# 脚本执行请求。

游戏退出请求只服务 MCP server 的 `requestQuit` 停止策略。前端收到请求后切到 Unity 主线程，设置 `GameApp.ReadyToQuit` 并调用
`GameApp.QuitGame()`；进程消失等待和强杀策略不归前端模块。

`entryThread = mainThread` 的脚本入口会切到 Unity 主线程。Unity 主线程分派复用
`shared/Wanxiang.Taiwu.DynamicScripting.Frontend`。

脚本引用选项由本模块创建：脚本契约 DLL 路径来自 `src/Scripting/`，前端额外加入 `Wanxiang.Prelude`
运行时提供的 UniTask 编译引用。

入口契约、契约 DLL 引用路径和 Mod 侧响应映射归 `src/Scripting/`，通用编译和临时程序集依赖解析归 shared
动态脚本运行核心。

前端游戏进程不作为 agent 可直接连接入口；agent 仍只连接 MCP server。
