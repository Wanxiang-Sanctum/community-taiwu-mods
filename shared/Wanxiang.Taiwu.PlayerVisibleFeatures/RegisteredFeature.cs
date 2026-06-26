namespace Wanxiang.Taiwu.PlayerVisibleFeatures;

internal sealed class RegisteredFeature(
    short featureId,
    int characterId)
{
    internal short FeatureId { get; } = featureId;

    internal int CharacterId { get; } = characterId;
}
