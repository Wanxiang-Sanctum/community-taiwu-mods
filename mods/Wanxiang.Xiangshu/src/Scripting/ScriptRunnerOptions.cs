using Wanxiang.Taiwu.DynamicScripting;

namespace Wanxiang.Xiangshu.Scripting;

public sealed class ScriptRunnerOptions
{
    public ScriptRunnerOptions(
        string side,
        DynamicScriptReferenceOptions references)
    {
#if NET6_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        ArgumentNullException.ThrowIfNull(references);
#else
        if (string.IsNullOrWhiteSpace(side))
        {
            throw new ArgumentException("Side is required.", nameof(side));
        }

        if (references is null)
        {
            throw new ArgumentNullException(nameof(references));
        }
#endif

        Side = side;
        References = references;
    }

    public string Side { get; }

    public DynamicScriptReferenceOptions References { get; }
}
