# 后端模块结构

`src/Backend/` 是观象台后端插件入口。后端插件负责启动供 MCP server 调用的后端 MessagePipe IPC endpoint，
并在 `.guanxiangtai-runtime/ipc-endpoints.json` 中登记 role 为 `backend` 的内部入口。

后端 endpoint 当前只处理状态检测请求。它不向 agent 暴露直接连接地址，也不承载动态脚本、调试或游戏状态读写工具。
MCP client 仍只连接 MCP server。
