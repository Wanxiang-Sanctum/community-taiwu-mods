using Wanxiang.Taiwu.ItemGrafts.Contracts;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

/// <summary>
/// 配置把嫁接附加到已有宿主物品时的可选行为。
/// </summary>
public sealed class AttachmentOptions
{
    /// <summary>
    /// 获取或设置嫁接会话成功建立后推送的即时通知；为 null 时不推送。
    /// </summary>
    public GraftSuccessNotification? SuccessNotification { get; set; }

    /// <summary>
    /// 获取或设置后端为本会话报告宿主事件时调用的回调。
    /// </summary>
    public Action<GraftHostEventArgs>? OnHostEvent { get; set; }
}
