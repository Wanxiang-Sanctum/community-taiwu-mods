using GameData.Domains.Item;
using Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

/// <summary>
/// 描述可为嫁接宿主物品展示的前端操作。
/// </summary>
/// <param name="label">非空操作标签。</param>
/// <param name="execute">操作启用时，以当前宿主物品 key 调用的回调。</param>
/// <param name="isEnabled">操作当前是否可以调用。</param>
/// <param name="disabledReason">操作禁用时提供给 UI 的可选原因。</param>
public sealed class GraftOperation(
    string label,
    Action<ItemKey>? execute = null,
    bool isEnabled = true,
    string disabledReason = "")
{
    /// <summary>
    /// 获取规范化后的操作标签。
    /// </summary>
    public string Label { get; } = ValidateRequiredText(label, nameof(label));

    /// <summary>
    /// 获取该操作当前是否可以调用。
    /// </summary>
    public bool IsEnabled { get; } = isEnabled;

    /// <summary>
    /// 获取规范化后提供给 UI 的禁用原因；未提供原因时为空字符串。
    /// </summary>
    public string DisabledReason { get; } = NormalizeOptionalText(disabledReason);

    private Action<ItemKey>? Execute { get; } = ValidateExecute(execute, isEnabled, nameof(execute));

    /// <summary>
    /// 使用当前宿主物品 key 调用已启用的操作。
    /// </summary>
    /// <param name="hostKey">当前宿主物品 key。</param>
    /// <exception cref="InvalidOperationException">操作已禁用。</exception>
    public void Invoke(ItemKey hostKey)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("Item graft operation is disabled.");
        }

        _ = GraftHostValidation.ValidateKey(hostKey, nameof(hostKey));

        Execute!(hostKey);
    }

    private static string ValidateRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", parameterName);
        }

        return value.Trim();
    }

    private static string NormalizeOptionalText(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static Action<ItemKey>? ValidateExecute(
        Action<ItemKey>? execute,
        bool isEnabled,
        string parameterName)
    {
        if (isEnabled && execute is null)
        {
            throw new ArgumentNullException(parameterName, "Enabled operation must have an execute callback.");
        }

        return execute;
    }
}
