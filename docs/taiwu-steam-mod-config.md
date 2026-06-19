# 太吾游戏与 Steam Mod 配置

这份机制参考说明太吾绘卷游戏内 Mod 管理与 Steam Workshop 之间的配置语义，依据是太吾绘卷读取和写回
`Config.Lua` 的游戏行为，以及 Steam Workshop 的发布元数据语义。实际 Mod 取值、开发项目模板和发布流水线由各自
文档维护。

`Config.Lua` 是太吾绘卷读取的 Mod 清单，不是 Steam 自己定义的配置文件。它是一个返回 Lua table 的文件，放在
每个 Mod 目录根部，和 `Plugins/`、`Config/`、`Settings.Lua` 等运行目录并列。

Steam Workshop 相关字段会写在同一个 table 里，因为太吾内置的 Mod 管理和上传界面会用 Steam API 同步标题、
简介、标签、可见性、封面、依赖和更新日志。也就是说，`Config.Lua` 是太吾的 Mod 配置格式，其中包含一部分
Workshop 发布元数据。

## 相关文件边界

- `Config.Lua`：Mod 元数据、插件入口、用户设置定义、Workshop 发布元数据。
- `Settings.Lua`：玩家实际修改后的 Mod 设置值，由游戏写在 Mod 目录下。
- `Config/*.lua`：修改游戏配置表的独立机制，由 `ModConfigDataManager` 读取，和顶层 `Config.Lua` 是两套入口。
- `ModSettings.Lua`：游戏档案目录下的启用 Mod、排序、白名单和本地临时 FileId 缓存，由游戏维护。

## 字段说明

| 字段 | 类型 | 含义 |
| --- | --- | --- |
| `Title` | string | 游戏内和 Workshop 展示名称。 |
| `Source` | number | 太吾 `ModSource`：`0` 是本地/外部 Mod，`1` 是 Steam Workshop，`2` 是 DLC。游戏从本地 `Mod/` 目录读取时会把非 `0` 改回 `0` 并写回。 |
| `FileId` | number | `ModId` 的文件 id。Steam Mod 使用 Workshop `PublishedFileId`；本地 Mod 可为 `0`，游戏会按目录名生成并缓存临时 id。 |
| `Version` | string 或 number | Mod 版本。字符串会按 .NET `System.Version` 解析成 `ModId.Version`；点分数字版本可被直接解析。 |
| `GameVersion` | string | 该 Mod 记录的太吾游戏版本，用于过期判断和旧版插件兼容分支。游戏写回时会更新为当前游戏版本。 |
| `Author` | string | 作者名；上传时也会写入 Workshop metadata。 |
| `Description` | string | Mod 简介；上传时同步为 Workshop 描述。 |
| `Cover` | string 或 nil | 本地展示封面路径。上传时如果 `WorkshopCover` 为空，会尝试用它作为 Workshop 预览图。 |
| `WorkshopCover` | string 或 nil | Workshop 预览图路径；为空时回退到 `Cover`。 |
| `DetailImageList` | string list | Workshop/详情页附加预览图路径列表。字段缺省时，游戏写回会补成空表。 |
| `Visibility` | number | Workshop 可见性：`0` public，`1` friends only，`2` private，`3` unlisted。 |
| `TagList` | string list | Mod 标签；上传时同步到 Workshop tags，也用于游戏内 Workshop 过滤。 |
| `Dependencies` | number list | Workshop 依赖的 published file id 列表。它表达 Steam Workshop item 之间的依赖关系，不是 DLL 依赖清单。 |
| `DefaultSettings` | table list | Mod 设置项定义和默认值。玩家实际值写入 `Settings.Lua`，运行时进入 `SerializableModData`。 |
| `SettingGroups` | string list | 设置界面分组顺序；设置项的 `GroupName` 可引用这里的名字。 |
| `UpdateLogList` | table list | 太吾上传流程维护的更新日志历史，元素包含 `Timestamp` 和 `LogList`。 |
| `ChangeConfig` | bool | “修改游戏配置”风险标记。若 Mod 修改游戏配置表，开启后存档读取界面可在 Mod 缺失时提示风险。 |
| `HasArchive` | bool | “含有存档数据”风险标记。若 Mod 会向存档写入自身数据，开启后存档读取界面可在 Mod 缺失时提示风险。 |
| `NeedRestartWhenSettingChanged` | bool | 设置修改后是否需要重启。开启后玩家修改设置时，游戏会标记需要重启并弹出确认提示。 |
| `BackendPlugins` | string list | 后端插件入口 DLL，路径相对 `Plugins/`。太吾后端从这些入口加载插件。 |
| `BackendPluginsLegacy` | string list | 后端旧版插件入口兼容字段；当 legacy 列表存在且版本判断需要兼容入口时，游戏会回退使用。 |
| `BackendPatches` | string list | 后端 patch 清单字段。 |
| `FrontendPlugins` | string list | 前端插件入口 DLL，路径相对 `Plugins/`。太吾前端从这些入口加载插件。 |
| `FrontendPluginsLegacy` | string list | 前端旧版插件入口兼容字段；当 legacy 列表存在且版本判断需要兼容入口时，游戏会回退使用。 |
| `FrontendPatches` | string list | 前端 patch 清单字段。 |
| `EventPackages` | string list | 事件包 DLL 清单。后端会从 Mod 的 `Events/` 目录加载这些事件包。 |

## DefaultSettings

每个设置项都有这些公共字段：

| 字段 | 含义 |
| --- | --- |
| `SettingType` | 设置类型，只能是 `Toggle`、`ToggleGroup`、`InputField`、`Slider`、`Dropdown` 之一。 |
| `Key` | 运行时读取设置值的键名。前后端插件用 `ModManager.GetSetting` 或 `DomainManager.Mod.GetSetting` 读取。 |
| `DisplayName` | 设置界面显示名。 |
| `Description` | 设置说明。 |
| `GroupName` | 可选分组名；配合顶层 `SettingGroups` 使用。 |

各类型的专属字段：

| `SettingType` | 专属字段 | 值类型 |
| --- | --- | --- |
| `Toggle` | `DefaultValue` | bool |
| `InputField` | `DefaultValue` | string |
| `Dropdown` | `Options`、`DefaultValue` | `Options` 是 string list；`DefaultValue` 是选项索引。 |
| `ToggleGroup` | `Toggles`、`DefaultValue` | `Toggles` 是 string array；`DefaultValue` 是选项索引。 |
| `Slider` | `MinValue`、`MaxValue`、`StepSize`、`DefaultValue` | int；`StepSize` 省略时默认为 `1`。 |

`Dropdown`、`ToggleGroup` 和 `Slider` 的值会被游戏夹到合法范围内。玩家改值后，游戏把实际值写入
`Settings.Lua`，再同步给前后端运行时；插件收到设置更新时会调用 `OnModSettingUpdate`。

## Steam 关系

- `Source = 0` 表示本地/外部 Mod。游戏从本地 `Mod/` 目录读取时，会把非 `0` 的 `Source` 改回 `0` 并写回。
- `Source = 1` 表示 Steam Workshop Mod。上传成功后，太吾会把 Steam 返回的 published file id 写入 `FileId`。
- `Visibility`、`TagList`、`Dependencies`、`WorkshopCover`、`DetailImageList` 等字段会参与太吾内置 Workshop
  上传或展示流程。
- `Dependencies` 对应 Steam Workshop item 依赖。Steam 侧查询结果也会把远端依赖同步回太吾的 Mod 信息。
- `Author`、`ChangeConfig`、`HasArchive` 和 `NeedRestartWhenSettingChanged` 会被太吾写入 Workshop metadata；
  查询 Workshop item 时，太吾也会从 metadata 还原这些展示和风险标记。

## 维护依据

本文维护时核对太吾游戏侧的 `ModManager`、`ModInfoWithDisplayData`、`SteamManager`、
`GameData.Domains.Mod.ModInfo`、`ModSource`、`EModVisibility` 和设置项类型读取路径。组织内部维护者可以通过
[`taiwu-modkit`](https://github.com/Wanxiang-Sanctum/taiwu-modkit) 仓库根目录下的 `game/` 生成快照检索这些游戏侧
路径。太吾游戏版本更新后，如果 Mod 管理界面或 `Config.Lua` 字段发生变化，先更新对应游戏观察快照，再复核本文。
