# 仓库级文档入口

`docs/` 收纳跨具体 Mod 复用的仓库级说明，按文档责任分两类：

- 机制参考：解释太吾游戏、Steam Workshop 或外部平台本身的稳定语义，以对应系统或游戏快照为依据。
- 仓库经验：记录本仓库跨具体 Mod 复用的发布判断、维护约定和协作经验，以仓库内项目和流程为依据。

仓库级文档的标题和开头应能看出责任与依据。机制参考不承载实际 Mod 取值、仓库模板或发布流水线约定；
这些内容由具体 Mod README、目录级 README、根 README 或专门的仓库经验文档维护。

仓库内路径默认相对本仓库根目录；跨仓库引用必须在首次出现时给出可点击仓库名，并说明后续路径相对哪个仓库根目录。

具体 Mod 的玩法、运行链路和源码模块仍由 `mods/<ModName>/README.md` 及其子目录 README 维护；共享项目 API 和
部署建议仍由 `shared/<ProjectName>/README.md` 维护。

## 文档入口

| 文档 | 何时阅读 |
| --- | --- |
| [太吾游戏与 Steam Mod 配置](taiwu-game-steam-config-lua.md) | 理解太吾读取的 `Config.Lua`、用户设置、插件入口和 Steam Workshop 字段关系时。 |

新增仓库级文档时，先确定它是机制参考还是仓库经验，再选择依据和边界。只属于单个 Mod 的实现细节留在该 Mod 自己的
README 或 `mods/<ModName>/docs/` 目录。
