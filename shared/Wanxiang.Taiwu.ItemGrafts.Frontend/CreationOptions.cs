using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using Wanxiang.Taiwu.ItemGrafts.Contracts;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

/// <summary>
/// 配置创建新真实宿主物品并附加嫁接时的可选行为。
/// </summary>
public sealed class CreationOptions
{
    /// <summary>
    /// 获取或设置嫁接会话成功建立后推送的即时通知；为 null 时不推送。
    /// </summary>
    public GraftSuccessNotification? SuccessNotification { get; set; }

    /// <summary>
    /// 获取或设置从创建前后行囊快照中选择新建宿主物品的选择器；返回值会由创建动作校验为请求的宿主模板。
    /// </summary>
    public Func<IReadOnlyList<ItemDisplayData>, IReadOnlyList<ItemDisplayData>, ItemKey>? SelectCreatedHost { get; set; }

    /// <summary>
    /// 获取或设置后端为本会话报告宿主事件时调用的回调。
    /// </summary>
    public Action<GraftHostEventArgs>? OnHostEvent { get; set; }
}
