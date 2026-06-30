# 前端模块结构

`src/Frontend/` 是观象台前端插件入口。它负责这些事：

- 确保包内 MCP server 进程已启动。
- 启动供 MCP server 调用的前端 MessagePipe IPC endpoint，并在 `.guanxiangtai-runtime/ipc-endpoints.json` 中登记
  role 为 `frontend` 的内部入口。
- 承接前端侧状态检测和受信 C# 脚本执行请求。

`entryThread = mainThread` 的脚本入口会切到 Unity 主线程。Unity 主线程分派和前端显式能力引用解析（当前包括 UniTask）复用
`shared/Wanxiang.Taiwu.DynamicScripting.Frontend`；入口契约和 Mod 侧响应映射归 `src/Scripting/`，通用编译和临时程序集依赖解析归
shared 动态脚本运行核心。
前端游戏进程不作为 agent 可直接连接入口；agent 仍只连接 MCP server。
