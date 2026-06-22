# IPC 共享模块

`src/Ipc/` 是 MCP sidecar 调用前端、后端插件时共享的跨进程 MessagePipe 契约与本机 endpoint 辅助库。
本模块只承载 sidecar 到目标插件端的 IPC；前后端之间的相枢业务协作使用 shared `Wanxiang.Taiwu.ModRpc` 的方法和
通知边界。

职责：

- 定义 sidecar 到目标插件端的 MessagePipe 请求/响应 DTO；调用端与处理器注册处使用同一对类型。
- 定义受信脚本执行请求的 `script`、`arguments`、`entryThread` 字段，以及脚本运行响应的嵌套 MessagePack
  判别联合：`notInvoked(reason)` 或 `invoked(returnValue | exception)`。
- 定义玩家前端视图截图请求和 PNG 响应；这里只承载跨进程协议，截图语义归前端 `PlayerView/`。
- 维护前端、后端和 MCP server 的 endpoint manifest 注册与发现；manifest 用 endpoint `role` 区分进程角色。
- 定义 MCP sidecar、前端和 CLI 适配器共享的 transport、path、header 和环境变量名称；请求门禁由
  `src/McpServer/` 执行。
- 提供相枢运行目录、插件部署目录和本机 loopback endpoint 辅助方法。

协议约定：

- endpoint manifest 承载本机进程发现所需的 `role`、transport、地址和进程信息。
- endpoint manifest 是供前端、后端和 MCP server 共同读写的普通 JSON 文档，字段名固定为 camelCase。
  `netstandard2.1`、`net8.0` 和 `net10.0` 必须保持同一份 manifest 形态。
- MessagePack `[Key]` 是线上的字段编号；调整现有 DTO 时保留既有编号。消息语义改变时，同步修改调用端和处理器，
  必要时创建新的请求/响应类型。
- 接收方没有业务结果要返回时，使用 `IpcNoContentResponse`。响应字段只表达调用方会消费的业务结果。
- DTO 可以按能力切分文件；文件名已经提供上下文时，DTO 名不重复 `Ipc` 或能力名前缀，命名保留本协议内需要区分的动作
  和事件名。
- 跨进程语义归本模块；前端、后端和 MCP 模块只实现本侧处理器或调用端。

这个模块描述跨进程协议和共享基础设施。前端 UI、后端游戏逻辑、MCP 工具语义和本机 Agent 调用由对应
运行模块实现；脚本能访问哪些游戏 API 也由目标侧插件进程决定。

脚本执行本身归前端或后端 endpoint。工具意图、玩家目标判断和脚本编译引用规则由对应运行模块维护。
