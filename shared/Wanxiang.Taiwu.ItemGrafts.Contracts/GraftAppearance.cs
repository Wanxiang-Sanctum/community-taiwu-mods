namespace Wanxiang.Taiwu.ItemGrafts.Contracts;

public sealed class GraftAppearance(
    string? name = null,
    string? description = null,
    string? iconName = null,
    sbyte? grade = null)
{
    public string? Name { get; } = NormalizeOptionalText(name);

    public string? Description { get; } = NormalizeOptionalText(description);

    public string? IconName { get; } = NormalizeOptionalText(iconName);

    public sbyte? Grade { get; } = grade;

    private static string? NormalizeOptionalText(string? value)
    {
        string? normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
