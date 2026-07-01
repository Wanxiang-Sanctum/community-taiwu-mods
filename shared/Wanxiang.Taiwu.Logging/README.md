# Wanxiang.Taiwu.Logging

太吾绘卷 Mod 仓库内部共享日志项目。

这个库不是新的日志系统，也不写入独立日志文件。它只提供一层结构化调用 API，把消息和上下文格式化成
太吾游戏日志系统可接收的一行文本，底层仍调用 `GameData.Utilities.AdaptableLog`。

## 职责边界

游戏日志面向 Mod 作者定位本机运行问题，不面向玩家行为统计或使用分析。需要玩家立即看到并采取行动的信息，应通过
UI、即时通知或控制台提示表达，不要只放进游戏日志。

成功请求、轮询和心跳属于主路径，默认不作为日志事件；只有失败、状态转换、边界不可用或需要作者据此定位问题时才记录。
日志上下文应围绕复现和定位问题所需的技术状态，例如 endpoint、manifest、进程号、适配器、配置来源和失败原因。

玩家输入、对话正文、脚本内容、token 值、环境变量值和完整本地配置属于调用方、外部工具或用户交互边界；需要关联这类
边界时，记录稳定 ID、数量、来源、脱敏摘要或文件路径。

这个库不规定具体业务字段。日志事件选择、字段取舍和敏感内容边界由调用方所属模块维护。

## 调用方式

示例：

```csharp
private static readonly TaiwuLogger Log = TaiwuLogger.ForTag("Wanxiang.Xiangshu");

Log.Info(
    "前端插件已初始化",
    new
    {
        adapter = settings.Adapter,
    });

Log.Error(ex, "MCP sidecar 启动失败");
```

日志上下文建议使用匿名对象表达。运行时契约是 `context` 必须能通过 Newtonsoft.Json 序列化为 JSON
对象；标量值需要放进具名属性里，例如 `new { frame }`。枚举按字符串输出，值为 `null` 的属性会被省略，
循环引用会在上下文 JSON 中跳过。调用方不需要把上下文预先转换为 `JObject`、字典或字符串。

可恢复但仍需要作者关注的异常使用 `Warning(ex, message, context)`；会中断当前功能或让调用方无法继续的异常使用
`Error(ex, message, context)`。

## 输出形态

```text
前端插件已初始化 | {"adapter":"Codex"}
```

异常会作为同一个 JSON 对象的 `exceptionType`、`exceptionMessage` 和 `exception` 字段输出。JSON 使用
紧凑格式，避免一个日志事件跨多行。

## 开发

从仓库根目录构建：

```powershell
dotnet build shared/Wanxiang.Taiwu.Logging/Wanxiang.Taiwu.Logging.csproj
```

目标框架、Taiwu 引用和包引用写在 `.csproj`。
