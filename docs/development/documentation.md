# 文档分层与维护

本文面向维护本仓库文档结构的人，说明各文档入口的受众、所有权和同步规则。提交贡献前的协作入口仍是根
`CONTRIBUTING.md`。

## 实践依据

- GitHub 将
  [README 作为仓库展示和上手入口](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes)；
  本仓库根 `README.md` 因此承担对外形象、获取方式、使用边界、源码入口和对外 Mod 入口表。
- GitHub 会在 issue、pull request、contribute 页面和仓库侧栏展示
  [贡献指南](https://docs.github.com/en/communities/setting-up-your-project-for-healthy-contributions/setting-guidelines-for-repository-contributors)；
  根 `CONTRIBUTING.md` 因此保持为短入口和提交前契约，不承载完整维护手册。
- [Diataxis](https://diataxis.fr/) 按读者需求区分教程、操作指南、参考和解释；本仓库按技术玩家、贡献者、源码维护者和
  具体模块维护者拆分文档入口。

## 受众分层

- 根 `README.md` 面向外部技术玩家，说明仓库形象、获取方式、使用边界、源码入口和对外 Mod 入口表。
- 根 `CONTRIBUTING.md` 面向准备提交 issue、讨论或 PR 的贡献者，提供变更类型路由和提交前检查入口。
- `docs/README.md` 面向维护本仓库实际 Mod 的人，作为仓库级开发维护文档、机制参考和仓库经验的导航，不复制具体 Mod、共享项目或
  模板的可变清单。
- `docs/development/README.md` 面向源码维护者，说明构建、检查、打包、发布、新增项目、文档和仓库结构。
- `mods/README.md` 拥有实际 Mod 的一级目录索引和目录约定，以及所有 Mod 共同遵守的组包、插件入口、引用和依赖部署规则。
- `shared/README.md` 拥有内部共享项目的一级目录索引、目录约定、目标框架和共享项目引用边界。
- `docs/` 收纳维护本仓库实际 Mod 时使用的开发维护文档、机制参考和仓库经验。
- `tools/README.md` 和 `templates/README.md` 说明工具实现入口、模板变量和渲染规则。
- 具体 Mod 的 `README.md` 面向外部技术玩家，说明玩法、运行边界和配置入口。
- 具体 Mod 的 `DEVELOPMENT.md` 面向源码维护者，说明源码模块、组包内容、构建命令和内部设计入口。
- 共享项目和源码子目录的 README 说明各自的模块边界、API、部署建议和项目内约定。

## 放置规则

索引是否存在由文档责任决定。根 `README.md` 保留对外 Mod 入口和稳定源码入口；`mods/README.md` 和
`shared/README.md` 完整索引各自的一级子目录；`docs/README.md` 只索引仓库级文档。开发手册、机制参考和具体设计文档
需要项目发现时，链接目录级 README。

索引行只保留选择信息和稳定入口。实际 Mod 的玩家说明留在对应 `mods/<ModName>/README.md`；源码迭代说明留在对应
`mods/<ModName>/DEVELOPMENT.md`、`mods/<ModName>/docs/` 或源码子目录 README；共享库 API、运行时依赖和部署建议留在
对应 `shared/<ProjectName>/README.md`。

同一个命令或概念需要面向多个读者出现时，只在最近的拥有者文档解释语义；其它入口保留最短可执行路径、必要前置条件和链接。
例如根 README 保留获取与使用路径，`mods/README.md` 解释组包 item，`tools/README.md` 解释 CLI 实现入口。

新增仓库级文档时，先判断它是机制参考、仓库经验还是开发维护文档。机制参考要把依据归到太吾游戏本体、外部平台或公开
可观察行为；仓库经验要把依据归到本仓库流程；开发维护文档要归到 `docs/development/` 或其子目录。新增后同步
`docs/README.md` 的仓库级阅读入口。

只属于单个 Mod 的实现细节放在该 Mod 自己的 `DEVELOPMENT.md`、`mods/<ModName>/docs/` 或源码子目录 README。

## 边界规则

`tools/`、`templates/` 和共享 MSBuild 目标服务本仓库继续增长。需要说明这些辅助设施的角色时，放在
`docs/development/README.md`、`tools/README.md` 或 `templates/README.md`；具体 Mod 的 README 保持面向技术玩家。

文档中引用跨仓库路径时，首次出现应给出可点击仓库名，并说明后续路径相对哪个仓库根目录。引用组织内部仓库时，同时说明它
承载的是工具、包还是同步快照角色。

仓库内入口文档优先使用 Markdown 链接；示例路径、占位路径和 MSBuild item 名称使用行内代码。链接目标应指向拥有该规则的
README 或文档，不从父级文档直接链接到子模块内部实现细节，除非该实现文件就是维护入口。

新增、移除或重命名实际 Mod 时，同步更新 `mods/README.md` 的一级目录索引，以及该 Mod 自己的 README、维护入口、
`Config.Lua`、组包入口、lock file、解决方案注册和发布配置。只有这个 Mod 应进入根 README 的对外 Mod 入口表时，才同步更新
根 README。

新增、移除或重命名内部共享项目时，同步更新 `shared/README.md` 的一级目录索引，以及项目自己的 README、项目文件、
lock file、解决方案注册和引用方部署声明。现有项目以项目文件、`Taiwu.Mod.Pack.proj`、lock file、目录 README 和
解决方案注册为准；`templates/` 只用于创建新项目的初始骨架。

太吾游戏版本更新后，如果 Mod 管理界面、上传流程或 `Config.Lua` 字段发生变化，按新游戏版本复核相关机制参考。

## 模板和工具同步

新增或调整 CLI 命令时，同步更新：

- `tools/Taiwu.Mods.Cli/CommandLineOptions.cs` 中的命令、参数和帮助文本。
- `docs/development/README.md` 中的命令说明。
- 受影响目录自己的 README 或 `DEVELOPMENT.md`。

模板上下文变量或模板选择规则变化时，同步更新：

- `tools/Taiwu.Mods.Cli/TemplateRenderer.cs`。
- `templates/README.md` 中的模板变量表。
- 受影响的 `templates/*` 模板内容。

模板只描述新项目的初始骨架。现有项目的真实构建和组包约定以项目文件、`Taiwu.Mod.Pack.proj`、目录 README、lock file 和
解决方案注册为准。

模板会生成新项目文档和 `Config.Lua` 展示字段占位。调整文档关系、最小入口文案、组包入口说明或共享项目边界时，同步复核
`templates/README.md` 中列出的输出文案模板，让新项目沿用当前文档关系。

面向玩家的 README 模板保持克制，只说明 Mod 身份并指向源码维护入口；具体功能、安装方式、品牌表达、发布文案和信息结构
由具体 Mod 维护。面向源码维护者的模板说明构建和组包边界；模板维护细节归本仓库文档。

新增或调整组包 helper、项目默认组包目标或 `pack-mod` 读取项目包产物的流程时，同步复核 `mods/README.md` 中对
`TaiwuModPackFile`、`TaiwuModPackDirectory`、`TaiwuModPackProject`、`TaiwuModPackEntry` 和发布目录组包目标的说明。
`ResolveTaiwuModPackOutputs` 是 CLI 读取项目包产物的 MSBuild 边界；普通 Mod 文档只说明公开 item 和项目配置入口。

## 生成内容

需要更新 `taiwu-modkit` 的游戏观察快照时，在 `taiwu-modkit` 仓库内运行对应工具重新生成；不要在本仓库复制或手写快照内容。
