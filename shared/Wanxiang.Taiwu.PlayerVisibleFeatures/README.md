# Wanxiang.Taiwu.PlayerVisibleFeatures

玩家可见虚拟人物特性的前端显示适配项目。

本项目把调用方定义的虚拟人物特性追加到指定人物的人物特性列表。它只接入原生人物特性条目和 tooltip 的前端渲染：列表渲染时追加虚拟
ID，条目和 tooltip 渲染该 ID 时返回本项目运行期维护的 `CharacterFeatureItem`。

本项目不负责选择人物、决定注册时机或表达业务规则。人物 ID 由调用方在注册时明确传入；本项目不默认选择太吾或任何具体角色。

## 接入

本项目面向 `netstandard2.1` 前端插件，引用 `Taiwu.ModKit.References.Frontend` 和
`Taiwu.ModKit.References.Shared`。使用方在前端插件初始化时调用 `VisibleFeatures.Install(this)`，再用
`VisibleFeatures.Register(characterId, definition)` 为具体人物注册虚拟特性。

`Install(...)` 使用当前入口插件的 Mod ID 创建 Harmony owner；这个 Mod ID 只用于补丁归属，不是虚拟特性的业务参数。
重复调用 `Install(...)` 保持幂等。

`VisibleFeatures.Uninstall()` 会卸载人物特性列表渲染补丁，并清空注册状态和运行期显示项。

接入原生人物特性 UI 所需的虚拟特性 ID 由本项目在内部动态分配。`FeatureRegistration.FeatureId` 返回本次实际使用的 ID，
供调用方做日志或诊断；调用方不指定该 ID。

## 状态边界

本项目只修改前端 UI 显示链路中的状态：

- 人物特性滚动列表的当前显示 ID 列表。
- 滚动列表数量刷新。
- 本项目内存中的虚拟 ID 到运行期 `CharacterFeatureItem` 显示项映射。
- 原生人物特性条目和 tooltip 渲染虚拟 ID 时读取 `CharacterFeatureItem` 的返回值。

本项目不修改这些游戏状态：

- 人物真实 `FeatureIds`。
- `CharacterDisplayData.FeatureIds`、`FeatureMonitor.FeatureIds` 或后端 `Character` 数据。
- `CharacterFeature` 配置表、ref name map、extra item map 或配置文件。
- 其它前端代码对 `CharacterFeature` 的常规读取结果。
- 存档数据、后端结算、人物规则、战斗规则和事件选择状态。

## 公开 API

- `VisibleFeatures`：安装前端渲染补丁，并注册或注销指定人物的虚拟人物特性。
- `FeatureDefinition`：虚拟特性的玩家可见显示内容。
- `FeatureStyle`：组织并透传原生 `CharacterFeatureItem` 显示字段，包括类型、等级、期限和三组奖章布局。
- `FeatureRegistration`：本次注册句柄，用于注销；同时返回本次前端运行时使用的虚拟特性 ID。

本项目不向调用方暴露完整 `CharacterFeatureItem` 构造面。需要新增可复用显示能力时，优先在上述有限显示契约中加字段，
不要让使用方直接修改游戏配置表。

## 示例

前端初始化时安装共享显示层：

```csharp
VisibleFeatures.Install(this);
```

为调用方选定的人物注册一项虚拟人物特性：

```csharp
FeatureRegistration registration =
    VisibleFeatures.Register(
        targetCharacterId,
        new FeatureDefinition(
            "前端标记",
            "这是一项只在前端显示的人物特性。")
            .WithEffectDescription("它不写入人物真实特性，也不改变后端机制。"));

short runtimeFeatureId = registration.FeatureId;
```

具体选择哪个人物、什么时候注册或注销，属于调用方策略。本项目只在人物特性 UI 渲染到该人物 ID 时追加显示项。

需要沿用原生人物特性 UI 的类型、等级、期限或奖章布局时，通过 `FeatureStyle` 透传这些显示字段：

```csharp
using Config.ConfigCells.Character;

VisibleFeatures.Register(
    targetCharacterId,
    new FeatureDefinition(
        "醒目标记",
        "使用正面特性样式。")
            .WithEffectDescription("这仍然只是前端显示。")
            .WithStyle(
                new FeatureStyle(
                    ECharacterFeatureType.Good,
                    level: 1,
                    duration: 0,
                    featureMedals:
                    [
                        new FeatureMedals(["inc"]),
                        new FeatureMedals([]),
                        new FeatureMedals([]),
                    ])));
```

本项目会从高位正数区间分配一个当前空闲 ID。该 ID 只进入前端人物特性列表和本项目的运行期显示项读取路径。
