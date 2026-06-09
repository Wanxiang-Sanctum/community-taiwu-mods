# 相枢

太吾绘卷混沌愿望回应 Mod。

当前阶段只做游戏内 IPC 和 MCP proxy smoke demo：前端插件和后端插件分别启动一个仅绑定
`127.0.0.1` 的 MessagePipe TCP endpoint。两个 endpoint 都只暴露一个 ping 请求。前端插件
启动时会拉起 MCP server 代理进程；MCP server 由 Kestrel 直接绑定 `127.0.0.1:0`，启动后读取
实际监听端口并写入 manifest。MCP tools 再读取 manifest 发现前后端 endpoint，并转发 ping
请求，用来验证游戏外进程能够区分连接前端侧和后端侧。

这个阶段不实现聊天窗口、不做游戏状态修改，也不做外部业务服务对接。IPC endpoint 端口在插件
启动时分配，并写入用户本地应用数据目录下的 manifest：

```text
Taiwu/Wanxiang.Xiangshu/ipc-endpoints.json
```

manifest 只记录发现本机 endpoint 所需的最小信息：`side`、`transport`、`host`、`path`、
`port`、`processId` 和 `startedAtUtc`。MCP server 会以 `side = "mcp-server"`、
`transport = "mcp-streamable-http"`、`path = "/mcp"` 写入同一个 manifest。

MCP server 使用 `ModelContextProtocol.AspNetCore`，由前端插件传入父进程 PID。插件正常释放时会
结束 MCP server；前端进程退出时，MCP server 也会停止。当前暴露两个 demo tools：

- `xiangshu_list_endpoints`：列出 manifest 中仍存活的前端/后端 IPC endpoint。
- `xiangshu_ping_plugin`：向 `frontend` 或 `backend` endpoint 发送 MessagePipe ping。

## 开发

从仓库根目录构建插件项目：

```powershell
dotnet build mods/Wanxiang.Xiangshu/src/Frontend/Wanxiang.Xiangshu.Frontend.csproj
dotnet build mods/Wanxiang.Xiangshu/src/Backend/Wanxiang.Xiangshu.Backend.csproj
dotnet build mods/Wanxiang.Xiangshu/src/McpServer/Wanxiang.Xiangshu.McpServer.csproj
```

打包可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name Wanxiang.Xiangshu
```

`pack-mod` 会运行 `Taiwu.Mod.Pack.proj`，把 `Config.Lua`、前后端最终入口 DLL、声明复制的
IPC contract DLL，以及 MCP server 的 `win-x64` 发布目录组装到仓库根目录的
`artifacts/mods/Wanxiang.Xiangshu/`。前端插件从
`Processes/Wanxiang.Xiangshu.McpServer/Wanxiang.Xiangshu.McpServer.exe` 启动代理进程。IPC
contract DLL 作为独立文件部署，避免 MessagePipe 请求类型被合并改名。

插件项目的本地构建设置写在对应项目的 `Taiwu.Mod.props`。`src/Ipc/` 是前后端共用的
IPC contract、manifest 注册和本机 endpoint 辅助类库。

前端运行时依赖由 `Wanxiang.FrontendRuntime` 提供；发布后需要将该前置 mod 的 Steam Workshop
`FileId` 加入相枢的 `Dependencies`。相枢前端只部署自己的入口 DLL 和 `Wanxiang.Xiangshu.Ipc.dll`。
后端仍按自身 `net8.0` 运行时边界声明并部署 MessagePipe 相关依赖。

## 项目结构

- `Config.Lua`：游戏读取的 mod 配置。
- `Taiwu.Mod.Pack.proj`：最终可部署目录的组包声明。
- `src/Frontend/`：前端插件项目；当前启动前端 MessagePipe IPC endpoint。
- `src/Backend/`：后端插件项目；当前启动后端 MessagePipe IPC endpoint。
- `src/Ipc/`：Wanxiang.Xiangshu IPC contract、manifest 注册和本机 endpoint 辅助类库。
- `src/McpServer/`：游戏外 MCP proxy server；当前通过 MCP tools 代理调用前后端 IPC ping。
