# 前端模块结构

`src/Frontend/` 是观象台前端插件入口。它负责两件事：

- 确保包内 MCP server 进程已启动。
- 启动供 MCP server 调用的前端 MessagePipe IPC endpoint，并在 `.guanxiangtai-runtime/ipc-endpoints.json` 中登记
  role 为 `frontend` 的内部入口。

前端 endpoint 当前只处理状态检测请求。前端游戏进程不作为 agent 可直接连接入口；agent 仍只连接 MCP server。
