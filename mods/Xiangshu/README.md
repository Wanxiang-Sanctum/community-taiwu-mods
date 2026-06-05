# 相枢

太吾绘卷混沌愿望回应 Mod。

`Xiangshu` 的目标形态是：前端提供玩家可见的聊天窗口，玩家向相枢
agent 下达指令；agent 再根据需要通过 MCP 调用前端或后端能力。相枢可能用
扭曲的方式满足玩家的愿望，但具体执行路径应由 agent 编排，而不是由某个插件侧
直接承担完整交互。

当前阶段只搭建这条链路的前置能力：前端插件和后端插件分别启动一个 stdio MCP
server，并各自暴露一个最小占位工具，用来验证 agent 将来可以区分调用前端侧和
后端侧。现阶段没有聊天窗口，没有 agent 编排，也不修改游戏状态。

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

`pack-mod` 会把 `Config.Lua` 和插件 DLL 组装到仓库根目录的
`artifacts/mods/Xiangshu/`。

插件项目的本地构建设置写在对应项目的 `Taiwu.Mod.props`。

## 项目结构

- `Config.Lua`：游戏读取的 mod 配置。
- `src/Frontend/`：前端插件项目；当前只提供前端 MCP server 占位能力。
- `src/Backend/`：后端插件入口项目。
- `src/BackendAgentTools/`：后端侧 agent 工具项目，当前以 MCP server 承载，供后端入口打包合并。
