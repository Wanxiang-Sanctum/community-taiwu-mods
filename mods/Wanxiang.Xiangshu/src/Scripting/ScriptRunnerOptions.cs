using Wanxiang.Taiwu.DynamicScripting;

namespace Wanxiang.Xiangshu.Scripting;

public sealed class ScriptRunnerOptions
{
    public ScriptRunnerOptions(
        string side,
        IEnumerable<string>? referenceDirectories = null,
        IEnumerable<string>? assemblyReferencePaths = null)
    {
#if NET6_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
#else
        if (string.IsNullOrWhiteSpace(side))
        {
            throw new ArgumentException("Side is required.", nameof(side));
        }
#endif

        DynamicScriptReferenceOptions referenceOptions = new(
            referenceDirectories,
            assemblyReferencePaths);

        Side = side;
        References = referenceOptions;
    }

    public string Side { get; }

    public DynamicScriptReferenceOptions References { get; }
}
