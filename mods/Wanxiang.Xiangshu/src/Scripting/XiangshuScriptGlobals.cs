namespace Wanxiang.Xiangshu.Scripting;

public sealed class XiangshuScriptGlobals(
    string targetSide,
    IReadOnlyDictionary<string, string> arguments,
    CancellationToken cancellationToken)
{
    public string Side { get; } = targetSide;

    public IReadOnlyDictionary<string, string> Arguments { get; } = arguments;

    public CancellationToken CancellationToken { get; } = cancellationToken;
}
