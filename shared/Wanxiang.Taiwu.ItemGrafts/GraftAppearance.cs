namespace Wanxiang.Taiwu.ItemGrafts;

public sealed class GraftAppearance(
    string? name = null,
    string? description = null,
    string? iconName = null,
    sbyte? grade = null)
{
    public string? Name { get; } = NormalizeRequiredOverride(name, nameof(name));

    public string? Description { get; } = NormalizeOptionalOverride(description);

    public string? IconName { get; } = NormalizeIconName(iconName);

    public sbyte? Grade { get; } = ValidateGrade(grade, nameof(grade));

    public bool IsEmpty => Name is null
        && Description is null
        && IconName is null
        && Grade is null;

    private static string? NormalizeRequiredOverride(string? value, string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Override text must not be empty.", parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalOverride(string? value)
    {
        return value?.Trim();
    }

    private static string? NormalizeIconName(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static sbyte? ValidateGrade(sbyte? value, string parameterName)
    {
        if (value is < -1 or > 8)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Grade must be -1 or between 0 and 8.");
        }

        return value;
    }
}
