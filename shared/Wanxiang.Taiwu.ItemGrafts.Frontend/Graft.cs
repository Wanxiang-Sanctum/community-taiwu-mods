using GameData.Domains.Item;
using Wanxiang.Taiwu.ItemGrafts.Contracts;
using Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

/// <summary>
/// 表示附加到真实宿主物品上的嫁接前端状态。
/// </summary>
public sealed class Graft
{
    internal Graft(
        ItemKey hostKey,
        GraftAppearance appearance,
        GraftMenuMode menuMode,
        IReadOnlyList<GraftOperation> operations)
    {
        HostKey = GraftHostValidation.ValidateKey(hostKey, nameof(hostKey));
        HostId = new GraftHostId(HostKey);
        Appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
        MenuMode = ValidateMenuMode(menuMode, nameof(menuMode));
        Operations = CopyOperations(operations);
    }

    /// <summary>
    /// 获取当前可传给太吾游戏 API 的完整物品 key。
    /// </summary>
    public ItemKey HostKey { get; private set; }

    /// <summary>
    /// 获取跨可变物品状态变化保持不变的稳定宿主身份。
    /// </summary>
    public GraftHostId HostId { get; }

    /// <summary>
    /// 获取嫁接的前端显示覆盖信息。
    /// </summary>
    public GraftAppearance Appearance { get; }

    /// <summary>
    /// 获取嫁接操作与原生物品菜单的组合方式。
    /// </summary>
    public GraftMenuMode MenuMode { get; }

    /// <summary>
    /// 获取嫁接暴露的前端操作。
    /// </summary>
    public IReadOnlyList<GraftOperation> Operations { get; }

    /// <summary>
    /// 获取该嫁接是否至少暴露一个前端操作。
    /// </summary>
    public bool HasOperations => Operations.Count > 0;

    internal void UpdateHostKey(ItemKey hostKey)
    {
        if (!HostId.Matches(hostKey))
        {
            throw new ArgumentException("Host key does not belong to this graft.", nameof(hostKey));
        }

        HostKey = GraftHostValidation.ValidateKey(hostKey, nameof(hostKey));
    }

    internal static GraftOperation[] CopyOperations(
        IReadOnlyList<GraftOperation> operations)
    {
        if (operations is null)
        {
            throw new ArgumentNullException(nameof(operations));
        }

        GraftOperation[] copy = [.. operations];

        if (Array.Exists(copy, static operation => operation is null))
        {
            throw new ArgumentException("Graft operations must not contain null.", nameof(operations));
        }

        return copy;
    }

    internal static GraftMenuMode ValidateMenuMode(
        GraftMenuMode menuMode,
        string parameterName)
    {
        return menuMode switch
        {
            GraftMenuMode.Append => menuMode,
            GraftMenuMode.Replace => menuMode,
            _ => throw new ArgumentOutOfRangeException(parameterName, menuMode, "Unsupported graft menu mode."),
        };
    }
}
