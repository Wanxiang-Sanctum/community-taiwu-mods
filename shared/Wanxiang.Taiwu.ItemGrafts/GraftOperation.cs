using GameData.Domains.Item;

namespace Wanxiang.Taiwu.ItemGrafts;

public sealed class GraftOperation(
    string label,
    Action<ItemKey>? execute = null,
    bool isEnabled = true,
    string disabledReason = "")
{
    public string Label { get; } = ValidateRequiredText(label, nameof(label));

    public bool IsEnabled { get; } = isEnabled;

    public string DisabledReason { get; } = NormalizeOptionalText(disabledReason);

    private Action<ItemKey>? Execute { get; } = ValidateExecute(execute, isEnabled, nameof(execute));

    public void Invoke(ItemKey hostKey)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("Item graft operation is disabled.");
        }

        _ = Graft.ValidateHostKey(hostKey, nameof(hostKey));

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
