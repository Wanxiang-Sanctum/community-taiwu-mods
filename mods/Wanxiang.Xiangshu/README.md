# 相枢

太吾绘卷混沌愿望回应 Mod。

当前阶段只做游戏内 IPC smoke demo：前端插件和后端插件分别启动一个仅绑定
`127.0.0.1` 的 MessagePipe TCP endpoint。两个 endpoint 都只暴露一个 ping 请求，
用来验证游戏外进程能够区分连接前端侧和后端侧。

这个阶段不实现聊天窗口、不做游戏状态修改，也不做外部业务服务对接。endpoint 端口在插件
启动时分配，并写入用户本地应用数据目录下的 manifest：

```text
Taiwu/Wanxiang.Xiangshu/ipc-endpoints.json
```

manifest 只记录发现 IPC endpoint 所需的最小信息：`side`、`transport`、`host`、`port`、
`processId` 和 `startedAtUtc`。

## 开发

从仓库根目录构建插件项目：

```powershell
dotnet build mods/Wanxiang.Xiangshu/src/Frontend/Wanxiang.Xiangshu.Frontend.csproj
dotnet build mods/Wanxiang.Xiangshu/src/Backend/Wanxiang.Xiangshu.Backend.csproj
```

打包可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name Wanxiang.Xiangshu
```

`pack-mod` 会运行 `Taiwu.Mod.Pack.proj`，把 `Config.Lua`、前后端最终入口 DLL 和声明复制的
IPC contract DLL 组装到仓库根目录的 `artifacts/mods/Wanxiang.Xiangshu/`，避免 MessagePipe
请求类型被合并改名。

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
