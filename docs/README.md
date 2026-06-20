# 仓库级文档入口

`docs/` 收纳仓库维护者使用的开发维护文档、跨 Mod 机制参考和仓库经验。玩家安装、使用和风险边界从根 `README.md`
与具体 Mod 的 `README.md` 开始。

本目录按文档责任分三类：

- 开发维护：说明仓库维护者如何构建、检查、打包、发布、扩展项目和维护文档结构。
- 机制参考：解释太吾游戏、Steam Workshop 或外部平台本身的稳定语义，依据太吾游戏本体、对应平台和公开可观察行为；
  组织内部游戏快照用于维护者复核。
- 仓库经验：记录本仓库跨具体 Mod 复用的发布判断、维护约定和协作经验，以仓库内项目和流程为依据。

仓库级文档的标题和开头应能看出责任与依据。机制参考专注于系统语义；实际 Mod 取值、仓库模板或发布流水线约定由具体
Mod 的 `README.md`、`DEVELOPMENT.md`、目录级 README 或专门的仓库经验文档维护。

实际 Mod 和内部共享项目的一级目录索引分别由 `mods/README.md`、`shared/README.md` 拥有；仓库级开发手册和机制参考
需要项目发现时，链接这两个入口。

仓库内路径默认相对本仓库根目录；跨仓库引用必须在首次出现时给出可点击仓库名，并说明后续路径相对哪个仓库根目录。
引用组织内部仓库时，说明它承载的工具、包或同步快照角色，并把机制依据归到对应游戏、平台或本仓库流程。

其它文档拥有的内容：

- 具体 Mod 的玩家说明、配置和运行边界，归 `mods/<ModName>/README.md`；Mod 共同规则归 `mods/README.md`。
- 具体 Mod 的源码模块、内部设计和组包内容，归 `mods/<ModName>/DEVELOPMENT.md`、`mods/<ModName>/docs/` 或源码子目录
  README。
- 共享项目 API、事件选择和部署建议，归 `shared/<ProjectName>/README.md`。
- 创建/移除命令实现、模板变量和渲染规则，归 `tools/README.md`、`templates/README.md` 和
  `docs/development/documentation.md`。

## 文档入口

| 文档 | 何时阅读 |
| --- | --- |
| [开发维护入口](development/README.md) | 构建、检查、打包、发布、新增或移除项目，以及理解仓库结构时。 |
| [文档分层与维护](development/documentation.md) | 调整 README、DEVELOPMENT、docs、模板文档或贡献入口的受众和同步规则时。 |
| [太吾游戏 Mod 配置与 Steam 发布边界](taiwu-mod-steam-publishing-boundary.md) | 理解太吾读取的 `Config.Lua`、用户设置、插件入口、Steam Workshop 字段和上传内容边界时。 |
