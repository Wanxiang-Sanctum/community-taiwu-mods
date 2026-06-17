# 游戏知识检索指引

当玩家询问游戏机制、术语、物品/功法/建筑/门派等资料，或使用运行工具前需要先解析游戏内名称、说明、配置、
本地化、模板/显示辅助和百晓册内容时，读取本文件。当前角色、地点、背包、地图、界面状态等动态事实仍按
当前输入或运行工具读取。

普通相枢人设和口吻先走 `persona/README.md`；静态世界观背景先走 `lore/README.md`。需要游戏内官方
资料、配置表、本地化文本、模板/显示辅助或百晓册文本时，再使用这里的运行时知识入口。

## 优先检索顺序

优先走直接数据源，反射只用于补足定位：

1. 已经在当前输入、`persona/` 或 `lore/` 中明确的内容，直接使用。
2. 当前存档、界面、角色或物品实例会影响回答时，按 `RUNTIME_SCRIPTING.md` 选择前端或后端读取当前事实。
3. 需要稳定游戏知识时，优先查 `Config.*` 配置表、本地化、模板显示辅助和百晓册数据结构。
4. 直接入口失败、成员名不确定或游戏版本差异导致编译失败时，做小范围反射；反射限定到目标 namespace
   或类型名前缀，并返回候选名，不枚举全部程序集。

## 配置与本地化

配置表是机制、模板、基础数值和枚举关系的首选入口。常见访问形态是：

- `Config.SomeTable.Instance[...]`：按模板 id 或 refName 读取配置项。
- `Config.SomeTable.Instance`：遍历配置表，适合按名称、类型或条件搜索；配置基类可枚举，并提供 `Count`。
- `Config.SomeTable.Instance.GetAllKeys()`：列出模板 id。
- `Config.SomeTable.Instance.RefNameMap`、`GetItemId(refName)`、`GetRefName(templateId)`：在 refName 和模板 id 间转换。
- `Config.EncyclopediaTipLink.DefKey.*`、`Config.EncyclopediaTipLink.DefValue.*`：百晓册提示链接的稳定命名入口。
- `Config.EncyclopediaTipLinkItem.RefName`：链接到百晓册引用项名称。
- `GlobalConfig.Instance`：全局规则、倍率、上限和通用参数的常见入口。
- `LocalStringManager.Get(...)`、`LocalStringManager.GetFormat(...)`、`LanguageKey.*`：读取本地化文本。
- `LocalStringManager.CurLanguageType` 或 `GameDataExtensions.LocalStringManagerHelper.CurLanguageType`：判断当前语言。

物品、功法、角色显示名和界面展示文本常有专用辅助类型。先用配置表和已有显示辅助；找不到明确入口时，
再按具体类型名做小范围探测。

`LanguageKey.*.Tr()` 和 `LanguageKey.*.TrFormat(...)` 是前端代码里常见的本地化快捷写法。返回文本可能带
颜色、超链接或富文本标签；回答玩家时保留含义即可，不必暴露内部标签。

## 模板与显示辅助

常见配置表和显示辅助能直接回答大量“这是什么、有什么属性、如何显示”的问题：

- 功法、技艺、门派、人物特性、建筑、地区、资源、状态和事件通常先查对应 `Config.*.Instance`。
- 物品模板先用 `GameData.Domains.Item.ItemTemplateHelper`。常用方法包括 `GetTemplateDataAllKeys`、
  `CheckTemplateValid`、`GetName`、`GetDesc`、`GetFunctionDesc`、`GetItemSubType`、`GetGrade`、`GetGroupId`、
  `GetIcon`、`IsTransferable`、`IsStackable`。
- 物品 `itemType` 和 `templateId` 应来自当前 `ItemKey`、配置项、refName 转换或运行时查询；工作区只维护检索路线，
  数值枚举清单留给配置表和运行时事实。
- 前端显示可参考 `ItemUtils.GetItemColorName(...)`、`Colors.Instance.GradeColors` 等显示辅助；回答事实问题时，
  优先返回名称、品级和说明；颜色标记只作为显示辅助。

当前存档里的具体物品、角色、地图块、建筑和功法实例不是模板事实。需要回答“我现在有多少、在哪、是谁、装备了
什么”时，按 `RUNTIME_SCRIPTING.md` 走后端域读取当前状态，再用配置和显示辅助补名称。

## 百晓册入口

百晓册主要是前端运行时资料，适合查询官方机制说明、教程文字、表格和百科链接。查询文字和表格时优先读取
数据结构；打开 UI 属于玩家明确要求的界面操作。

常见 namespace 和类型：

- `Game.Views.Encyclopedia`：百晓册数据、节点、引用和面板入口。
- `Game.Views.Encyclopedia.Views`：百晓册 UI 搜索结果和展示视图。
- `EncyclopediaDataManager.Instance`：节点树入口。
- `EncyclopediaContent.Instance`：从当前语言的 `EncyclopediaAssets/EncyclopediaContent.tsv` 读取正文、标题、
  key、超链接和插入引用。
- `EncyclopediaReference.Instance`：从当前语言的 `EncyclopediaAssets/EncyclopediaReference.tsv` 读取表格、
  图片、超链接和引用参数。
- `EncyclopediaDataProcessor.GetTable(tableName)`：读取百晓册引用的表格数据。
- `NodeData`：百晓册节点，常用字段/属性包括 `Id`、`Key`、`Title`、`Content`、`Children`、`Parent`、
  `LevelOneRoot`、`ConfigItem`。
- `NodeData.Search(value, onlyTitle, includeChildren, includeContent)`：按百晓册节点标题、正文和插入表格查找文本。
- `IEncyclopediaSearchableContent`：标题/正文可搜索内容的共同形态。

百晓册面板操作属于前端 UI 行为。只有玩家明确要求打开、跳转或操作界面时，才考虑：

- `ViewEncyclopediaPanel.OpenLink(Config.EncyclopediaTipLink.DefValue.SomeTopic)`
- `UIManager.Instance.ShowUI(UIElement.Encyclopedia)`

普通问答直接读取数据结构并返回简洁结果。

## 查询结果

本文件只说明检索路线和结果边界，不提供具体脚本写法。查询时优先返回少量结构化事实，再由相枢组织成玩家
可读答复。

- 百晓册查询返回候选节点、key、标题、正文摘要、表格名或匹配来源即可；结果范围限制在当前问题需要的资料。
- 配置查询先确定表名、refName、模板 id 或目标类型，再读取必要字段；结果范围限制在当前问题需要的配置项。
- 模板/显示查询返回名称、说明、品级、子类、图标名等必要事实；需要 `itemType` 时，从当前 `ItemKey` 或配置
  查询得到；数值映射以运行时事实为准。

## 选择目标侧

- 查百晓册正文、百晓册表格、百晓册 UI 链接或当前界面状态：优先 `frontend`。
- 查当前存档、角色、地图、物品实例、后端域或会改变世界状态的操作：优先 `backend`，必要时再用前端显示辅助
  补名称。
- 查纯配置表、本地化或模板资料：两侧都可能可用；优先选离当前问题最近的一侧，失败后再换侧。
- 查物品模板名称、描述、品级和子类：优先用 `ItemTemplateHelper`，再查具体 `Config.*` 表。

目标侧不确定时，先运行最小只读查询。编译失败时，只调整 using、目标侧或类型名；反射范围保持在当前问题相关类型内。

## 反射边界

反射是定位缺失成员的补位手段，默认路径仍是直接入口。

- 限定到 `Game.Views.Encyclopedia`、`Config`、`GameData.Domains` 等目标 namespace。
- 限定类型名前缀，例如 `Encyclopedia`、`CombatSkill`、`Item`、`LocalString`。
- 探测代码保持只读。
- 反射输出先整理为候选类型名、属性名或方法签名摘要，再转成玩家可理解的答复。
