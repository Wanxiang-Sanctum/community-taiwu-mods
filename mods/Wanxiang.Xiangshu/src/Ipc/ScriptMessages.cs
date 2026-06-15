using System.Collections.ObjectModel;
using MessagePack;

namespace Wanxiang.Xiangshu.Ipc;

[MessagePackObject]
public sealed class IpcRunScriptRequest(
    string script,
    IReadOnlyDictionary<string, string>? arguments)
{
    private static readonly IReadOnlyDictionary<string, string> EmptyArguments =
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(StringComparer.Ordinal));

    [Key(0)]
    public string Script { get; } =
        script ?? throw new ArgumentNullException(nameof(script));

    [Key(1)]
    public IReadOnlyDictionary<string, string> Arguments { get; } =
        arguments is { Count: > 0 }
            ? new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(arguments, StringComparer.Ordinal))
            : EmptyArguments;
}

[MessagePackObject]
public sealed class IpcRunScriptResponse(
    string returnValueJson,
    string error,
    IReadOnlyList<string>? diagnostics)
{
    private static readonly IReadOnlyList<string> EmptyDiagnostics =
        Array.AsReadOnly(Array.Empty<string>());

    [Key(0)]
    public string ReturnValueJson { get; } =
        returnValueJson ?? throw new ArgumentNullException(nameof(returnValueJson));

    [Key(1)]
    public string Error { get; } =
        error ?? throw new ArgumentNullException(nameof(error));

    [Key(2)]
    public IReadOnlyList<string> Diagnostics { get; } =
        diagnostics is { Count: > 0 }
            ? Array.AsReadOnly([.. diagnostics])
            : EmptyDiagnostics;
}
