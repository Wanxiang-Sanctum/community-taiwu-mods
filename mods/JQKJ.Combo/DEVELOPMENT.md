# 界青快剑-连击版 开发说明

## 架构

纯后端插件，通过 Harmony 前缀补丁拦截 `JieQingKuaiJian.OnCastSkillEnd` 和 `JieQingKuaiJian.OnPrepareSkillBegin`，
以及后缀补丁拦截 `CombatDomain.EndCombat`，实现连击逻辑。

## 核心逻辑

| 文件 | 作用 |
| --- | --- |
| `JieQingComboPlugin.cs` | 插件入口，Harmony 补丁，连击状态管理 |

- `PrefixCastSkillEnd`：施展技能结束时判定连击几率，首次施展发放正逆练杀式，连击成功补 1 杀式维持连击链，触发下一次免费施展
- `PrefixPrepareSkillBegin`：连击状态下按连击数提升释放进度
- `PostfixCombatEnd`：战斗结束时重置连击状态和几率

## 构建

```powershell
dotnet build mods/JQKJ.Combo/src/Backend/JQKJ.Combo.Backend.csproj
```

## 打包

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name JQKJ.Combo
```
