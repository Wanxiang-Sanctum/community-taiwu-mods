# 后端模块结构

`src/Backend/` 是观象台后端插件入口。后端插件负责启动供 MCP server 调用的后端 MessagePipe IPC endpoint，
并在 `.guanxiangtai-runtime/ipc-endpoints.json` 中登记 role 为 `backend` 的内部入口。

后端 endpoint 处理状态检测和受信 C# 脚本执行请求。`entryThread = mainThread` 的脚本入口会通过
`shared/Wanxiang.Taiwu.DynamicScripting.Backend` 排入 GameData 主循环。入口契约和 Mod 侧响应映射归
`src/Scripting/`，通用编译和临时程序集依赖解析归 shared 动态脚本运行核心。它不向 agent 暴露直接连接地址；MCP client 仍只连接 MCP server。
