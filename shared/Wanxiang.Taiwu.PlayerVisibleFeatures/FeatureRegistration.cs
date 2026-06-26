namespace Wanxiang.Taiwu.PlayerVisibleFeatures;

/// <summary>
/// 表示一项已注册的玩家可见虚拟人物特性。
/// </summary>
public sealed class FeatureRegistration
{
    internal FeatureRegistration(
        long registrationId,
        short featureId)
    {
        RegistrationId = registrationId;
        FeatureId = featureId;
    }

    internal long RegistrationId { get; }

    /// <summary>
    /// 本次前端运行时用于接入人物特性 UI 的虚拟特性 ID。
    /// </summary>
    public short FeatureId { get; }
}
