# 相枢

太吾绘卷混沌愿望回应 Mod。

`Xiangshu` 的目标形态是：前端提供玩家可见的聊天窗口，玩家向相枢
agent 下达指令；agent 再根据需要通过 MCP 调用前端或后端能力。相枢可能用
扭曲的方式满足玩家的愿望，但具体执行路径应由 agent 编排，而不是由某个插件侧
直接承担完整交互。

当前阶段只搭建这条链路的前置能力：前端插件和后端插件分别启动一个仅绑定
`127.0.0.1` 的 Streamable HTTP MCP endpoint，并各自暴露一个最小占位工具，用来
验证外部 agent 将来可以区分调用前端侧和后端侧。现阶段没有聊天窗口，没有 agent
编排，也不修改游戏状态。

endpoint 端口在插件启动时分配，并写入本地 manifest，供游戏外部 agent 注册。manifest
位于用户本地应用数据目录下：

```text
Taiwu/Xiangshu/mcp-endpoints.json
```

manifest 中的 endpoint 需要携带对应 `Authorization` header 调用。

## 开发

从仓库根目录构建插件项目：

```powershell
dotnet build mods/Xiangshu/src/Frontend/Xiangshu.Frontend.csproj
dotnet build mods/Xiangshu/src/Backend/Xiangshu.Backend.csproj
```

打包可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name Xiangshu
```

`pack-mod` 会把 `Config.Lua` 和前后端最终入口 DLL 组装到仓库根目录的
`artifacts/mods/Xiangshu/`。MCP 共享库和运行时依赖由前端、后端入口项目分别合并进各自的入口
DLL。

插件项目的本地构建设置写在对应项目的 `Taiwu.Mod.props`。`src/Mcp/` 是前后端共用的内部类库；
需要合并的 MCP 依赖由前端和后端入口项目分别声明。

## 项目结构

- `Config.Lua`：游戏读取的 mod 配置。
- `src/Frontend/`：前端插件项目；当前拥有前端 MCP endpoint 占位工具。
- `src/Backend/`：后端插件项目；当前拥有后端 MCP endpoint 占位工具。
- `src/Mcp/`：Xiangshu 内部 MCP transport、工具标记和 manifest 注册类库，供前后端项目复用。
