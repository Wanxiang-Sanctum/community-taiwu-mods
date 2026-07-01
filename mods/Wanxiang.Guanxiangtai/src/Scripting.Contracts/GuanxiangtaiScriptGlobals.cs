using Newtonsoft.Json.Linq;

namespace Wanxiang.Guanxiangtai.Scripting;

public sealed class GuanxiangtaiScriptGlobals(
    string side,
    JObject arguments,
    CancellationToken cancellationToken)
{
    public string Side { get; } = side;

    public JObject Arguments { get; } = CloneArguments(arguments);

    public CancellationToken CancellationToken { get; } = cancellationToken;

    private static JObject CloneArguments(JObject arguments)
    {
        return arguments is null
            ? throw new ArgumentNullException(nameof(arguments))
            : (JObject)arguments.DeepClone();
    }
}
