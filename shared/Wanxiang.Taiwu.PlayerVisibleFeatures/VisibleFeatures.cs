using Config;
using Config.ConfigCells.Character;
using TaiwuModdingLib.Core.Plugin;

namespace Wanxiang.Taiwu.PlayerVisibleFeatures;

/// <summary>
/// 提供玩家可见虚拟人物特性的共享前端入口。
/// </summary>
public static class VisibleFeatures
{
    private const short FirstAutoFeatureId = 30000;

    private static readonly object SyncRoot = new();

    private static readonly Dictionary<string, short> FeatureIdsByDisplaySignature = [];

    private static bool s_isInstalled;

    /// <summary>
    /// 安装共享前端渲染补丁。
    /// </summary>
    /// <param name="plugin">前端插件实例。</param>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> 为 null。</exception>
    public static void Install(TaiwuRemakePlugin plugin)
    {
        if (plugin is null)
        {
            throw new ArgumentNullException(nameof(plugin));
        }

        lock (SyncRoot)
        {
            if (s_isInstalled)
            {
                return;
            }

            string ownerModId = ValidateOwnerModId(plugin.ModIdStr);
            FeatureDisplayLayer.Install(ownerModId);
            s_isInstalled = true;
        }
    }

    /// <summary>
    /// 卸载共享前端渲染补丁，并清空本次注册的虚拟特性。
    /// </summary>
    /// <returns>存在已安装状态或注册状态时返回 true；否则返回 false。</returns>
    public static bool Uninstall()
    {
        lock (SyncRoot)
        {
            bool wasInstalled = s_isInstalled
                || FeatureDisplayLayer.IsInstalled
                || FeatureDisplayState.HasState;

            s_isInstalled = false;
            FeatureIdsByDisplaySignature.Clear();
            FeatureDisplayLayer.Uninstall();
            FeatureDisplayState.Clear();
            return wasInstalled;
        }
    }

    /// <summary>
    /// 为指定人物注册一项玩家可见虚拟人物特性。
    /// </summary>
    /// <param name="characterId">要显示虚拟人物特性的人物 ID。</param>
    /// <param name="definition">虚拟人物特性定义。</param>
    /// <returns>注册结果，可用于注销本次注册。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterId"/> 小于 0。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="definition"/> 为 null。</exception>
    /// <exception cref="InvalidOperationException">没有可用的前端虚拟特性 ID。</exception>
    public static FeatureRegistration Register(
        int characterId,
        FeatureDefinition definition)
    {
        int validatedCharacterId = ValidateCharacterId(characterId, nameof(characterId));

        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        lock (SyncRoot)
        {
            EnsureInstalled();
            short featureId = EnsureDisplayItem(definition);
            long registrationId = FeatureDisplayState.Register(
                featureId,
                validatedCharacterId);
            return new FeatureRegistration(
                registrationId,
                featureId);
        }
    }

    /// <summary>
    /// 取消注册一项玩家可见虚拟人物特性。
    /// </summary>
    /// <param name="registration">注册结果。</param>
    /// <returns>找到并移除注册项时返回 true；否则返回 false。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="registration"/> 为 null。</exception>
    public static bool Unregister(FeatureRegistration registration)
    {
        if (registration is null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        lock (SyncRoot)
        {
            return FeatureDisplayState.Unregister(registration.RegistrationId);
        }
    }

    private static short EnsureDisplayItem(FeatureDefinition definition)
    {
        CharacterFeature characterFeatures = CharacterFeature.Instance;
        string displaySignature = definition.GetDisplaySignature();

        if (FeatureIdsByDisplaySignature.TryGetValue(displaySignature, out short existingFeatureId))
        {
            CharacterFeatureItem existingItem = GetRequiredDisplayItem(existingFeatureId);
            ApplyDefinition(existingItem, existingFeatureId, definition);
            return existingFeatureId;
        }

        short featureId = AllocateFeatureId(characterFeatures);
        CharacterFeatureItem displayItem = new();
        ApplyDefinition(displayItem, featureId, definition);
        FeatureDisplayState.SetDisplayItem(featureId, displayItem);
        FeatureIdsByDisplaySignature[displaySignature] = featureId;
        return featureId;
    }

    private static CharacterFeatureItem GetRequiredDisplayItem(short featureId)
    {
        if (!FeatureDisplayState.TryGetDisplayItem(featureId, out CharacterFeatureItem item))
        {
            throw new InvalidOperationException("Virtual character feature display item is missing.");
        }

        return item;
    }

    private static void ApplyDefinition(
        CharacterFeatureItem item,
        short featureId,
        FeatureDefinition definition)
    {
        FeatureStyle style = definition.Style;

        item.Modify(nameof(CharacterFeatureItem.TemplateId), featureId);
        item.Modify(nameof(CharacterFeatureItem.Name), definition.Name);
        item.Modify(nameof(CharacterFeatureItem.SmallVillageName), string.Empty);
        item.Modify(nameof(CharacterFeatureItem.Hidden), false);
        item.Modify(nameof(CharacterFeatureItem.BelongAdventure), false);
        item.Modify(nameof(CharacterFeatureItem.Type), style.FeatureType);
        item.Modify(nameof(CharacterFeatureItem.Desc), definition.Description);
        item.Modify(nameof(CharacterFeatureItem.SmallVillageDesc), string.Empty);
        item.Modify(nameof(CharacterFeatureItem.EffectDesc), definition.EffectDescription);
        item.Modify(nameof(CharacterFeatureItem.FeatureMedals), CreateEmptyFeatureMedals());
        item.Modify(nameof(CharacterFeatureItem.Level), style.Level);
        item.Modify(nameof(CharacterFeatureItem.Duration), style.Duration);
        item.Modify(nameof(CharacterFeatureItem.MutexGroupId), featureId);
    }

    private static short AllocateFeatureId(CharacterFeature characterFeatures)
    {
        for (int featureId = FirstAutoFeatureId; featureId <= short.MaxValue; featureId++)
        {
            short candidate = (short)featureId;
            if (!IsFeatureIdInUse(characterFeatures, candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No available virtual CharacterFeature id remains.");
    }

    private static bool IsFeatureIdInUse(
        CharacterFeature characterFeatures,
        short featureId)
    {
        if (featureId < characterFeatures.Count
            || FeatureDisplayState.ContainsVirtualFeatureId(featureId))
        {
            return true;
        }

        foreach (int usedFeatureId in characterFeatures.RefNameMap.Values)
        {
            if (usedFeatureId == featureId)
            {
                return true;
            }
        }

        return false;
    }

    private static FeatureMedals[] CreateEmptyFeatureMedals()
    {
        FeatureMedals[] featureMedals = new FeatureMedals[FeatureMedalType.Count];

        for (int i = 0; i < featureMedals.Length; i++)
        {
            featureMedals[i] = new FeatureMedals();
        }

        return featureMedals;
    }

    private static int ValidateCharacterId(
        int characterId,
        string paramName)
    {
        if (characterId < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, characterId, "Character id must not be negative.");
        }

        return characterId;
    }

    private static void EnsureInstalled()
    {
        if (!s_isInstalled)
        {
            throw new InvalidOperationException(
                "VisibleFeatures.Install(plugin) must be called before registering visible features.");
        }
    }

    private static string ValidateOwnerModId(string modId)
    {
        if (string.IsNullOrWhiteSpace(modId))
        {
            throw new InvalidOperationException("Mod id is not available.");
        }

        return modId;
    }
}
