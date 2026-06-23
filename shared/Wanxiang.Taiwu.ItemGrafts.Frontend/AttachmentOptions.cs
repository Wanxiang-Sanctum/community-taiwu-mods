using Wanxiang.Taiwu.ItemGrafts.Contracts;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

/// <summary>
/// 配置把嫁接附加到已有宿主物品时的可选行为。
/// </summary>
public sealed class AttachmentOptions
{
    /// <summary>
    /// 获取或设置会话创建后推送的即时通知文本；为 null 时不推送，非 null 时不能为空白。
    /// </summary>
    public string? NotificationMessage { get; set; }

    /// <summary>
    /// 获取或设置与 <see cref="NotificationMessage"/> 搭配使用的原生即时通知记录类型。
    /// </summary>
    public short NotificationRecordType { get; set; } = GraftNotifications.DefaultNativeRecordType;

    /// <summary>
    /// 获取或设置后端为本会话报告宿主事件时调用的回调。
    /// </summary>
    public Action<GraftHostEventArgs>? OnHostEvent { get; set; }
}
