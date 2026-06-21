namespace Wanxiang.Taiwu.ItemGrafts.Contracts;

/// <summary>
/// 标识嫁接宿主事件报告的宿主事实种类。
/// </summary>
public enum GraftHostEventKind
{
    /// <summary>
    /// 真实宿主物品已从游戏状态中移除。
    /// </summary>
    Removed = 0,

    /// <summary>
    /// 真实宿主物品进入、离开或转移于角色行囊之间。
    /// </summary>
    LocationChanged = 1,

    /// <summary>
    /// 真实宿主物品数据已变化，调用方应按需重新查询。
    /// </summary>
    DataChanged = 2,
}
