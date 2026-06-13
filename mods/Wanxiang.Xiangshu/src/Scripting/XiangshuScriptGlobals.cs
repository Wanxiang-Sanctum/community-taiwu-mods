namespace Wanxiang.Xiangshu.Scripting;

public sealed class XiangshuScriptGlobals(
    string side,
    IReadOnlyDictionary<string, string> arguments,
    CancellationToken cancellationToken)
{
    public string Side { get; } = side;

    public IReadOnlyDictionary<string, string> Arguments { get; } = arguments;

    public CancellationToken CancellationToken { get; } = cancellationToken;
}
