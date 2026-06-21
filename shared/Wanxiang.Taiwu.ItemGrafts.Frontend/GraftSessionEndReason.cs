namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

/// <summary>
/// 描述嫁接会话停止活动的原因。
/// </summary>
public enum GraftSessionEndReason
{
    /// <summary>
    /// 调用方释放了会话。
    /// </summary>
    Canceled = 0,

    /// <summary>
    /// 后端报告真实宿主物品已被移除。
    /// </summary>
    HostRemoved = 1,
}
