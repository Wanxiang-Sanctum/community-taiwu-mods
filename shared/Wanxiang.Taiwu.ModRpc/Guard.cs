namespace Wanxiang.Taiwu.ModRpc;

internal static class Guard
{
    internal static void ThrowIfNull<T>(T? value, string parameterName)
        where T : class
    {
#if NET8_0
        ArgumentNullException.ThrowIfNull(value, parameterName);
#else
#pragma warning disable RCS1256
        if (value is null)
#pragma warning restore RCS1256
        {
            throw new ArgumentNullException(parameterName);
        }
#endif
    }

    internal static string RequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", parameterName);
        }

        return value.Trim();
    }
}
