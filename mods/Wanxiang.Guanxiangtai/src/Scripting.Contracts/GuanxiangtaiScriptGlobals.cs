namespace Wanxiang.Guanxiangtai.Scripting;

public sealed class GuanxiangtaiScriptGlobals(
    string side,
    IReadOnlyDictionary<string, string> arguments,
    CancellationToken cancellationToken)
{
    public string Side { get; } = side;

    public IReadOnlyDictionary<string, string> Arguments { get; } = arguments;

    public CancellationToken CancellationToken { get; } = cancellationToken;
}
