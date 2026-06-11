# Wanxiang.Taiwu.Logging

太吾绘卷 mod 仓库内部共享日志项目。

这个库不是新的日志系统，也不写入独立日志文件。它只提供一层结构化调用 API，把消息和上下文格式化成
太吾游戏日志系统可接收的一行文本，底层仍调用 `GameData.Utilities.AdaptableLog`。

## 调用方式

示例：

```csharp
private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

Log.Info(
    "frontend plugin initialized",
    new
    {
        adapter = settings.Adapter,
        workingDirectory = settings.WorkingDirectory,
    });

Log.Error(ex, "MCP sidecar failed to start");
```

日志上下文建议使用匿名对象表达。运行时契约是 `context` 必须能通过 Newtonsoft.Json 序列化为 JSON
对象；标量值需要放进具名属性里，例如 `new { frame }`。调用方不需要把上下文预先转换为 `JObject`、
字典或字符串；游戏运行时兼容性由这个共享库内部处理。

## 输出形态

```text
frontend plugin initialized | {"adapter":"Codex","workingDirectory":"AgentWorkspace"}
```

异常会作为同一个 JSON 对象的 `exceptionType`、`exceptionMessage` 和 `exception` 字段输出。JSON 使用
紧凑格式，避免一个日志事件跨多行。

## 开发

从仓库根目录构建：

```powershell
dotnet build shared/Wanxiang.Taiwu.Logging/Wanxiang.Taiwu.Logging.csproj
```

目标框架、Taiwu 引用和包引用写在 `.csproj`。

共享项目不作为独立插件入口写入 mod 包。引用它的前端或后端插件项目需要在 `Taiwu.Mod.props` 中声明
依赖部署动作；通常应把 `Wanxiang.Taiwu.Logging.dll` 作为 merge dependency。
