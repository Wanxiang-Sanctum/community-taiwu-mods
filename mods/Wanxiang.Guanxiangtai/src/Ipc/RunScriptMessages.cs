using System.Collections.ObjectModel;
#if NET10_0_OR_GREATER
using System.Text;
using System.Text.Json;
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif
using MessagePack;

namespace Wanxiang.Guanxiangtai.Ipc;

[MessagePackObject]
public sealed class IpcRunScriptRequest(
    string script,
    string argumentsObjectJson,
    IpcScriptEntryThread entryThread = IpcScriptEntryThread.Current)
{
    [Key(0)]
    public string Script { get; } =
        script ?? throw new ArgumentNullException(nameof(script));

    [Key(1)]
    public string ArgumentsObjectJson { get; } = NormalizeArgumentsObjectJson(argumentsObjectJson);

    [Key(2)]
    public IpcScriptEntryThread EntryThread { get; } =
        ValidateEntryThread(entryThread);

    private static IpcScriptEntryThread ValidateEntryThread(
        IpcScriptEntryThread entryThread)
    {
        return entryThread is IpcScriptEntryThread.Current
                or IpcScriptEntryThread.MainThread
            ? entryThread
            : throw new ArgumentOutOfRangeException(
                nameof(entryThread),
                entryThread,
                "Unsupported script entry thread.");
    }

    private static string NormalizeArgumentsObjectJson(string argumentsObjectJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsObjectJson))
        {
            throw new ArgumentException(
                "Script arguments must be a JSON object.",
                nameof(argumentsObjectJson));
        }

        try
        {
#if NET10_0_OR_GREATER
            using JsonDocument document = JsonDocument.Parse(argumentsObjectJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException(
                    "Script arguments must be a JSON object.",
                    nameof(argumentsObjectJson));
            }

            return WriteNormalizedJson(document.RootElement);
#else
            return JObject.Parse(argumentsObjectJson).ToString(Formatting.None);
#endif
        }
        catch (JsonException ex)
        {
            throw new ArgumentException(
                "Script arguments must be a valid JSON object.",
                nameof(argumentsObjectJson),
                ex);
        }
    }

#if NET10_0_OR_GREATER
    private static string WriteNormalizedJson(JsonElement element)
    {
        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream);

        element.WriteTo(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }
#endif
}

public enum IpcScriptEntryThread
{
    Current = 0,

    MainThread = 1,
}

[Union(0, typeof(IpcRunScriptNotInvokedResponse))]
[Union(1, typeof(IpcRunScriptInvokedResponse))]
[MessagePackObject]
public abstract class IpcRunScriptResponse
{
    public static IpcRunScriptResponse InvokedWithReturnValue(string returnValueJson)
    {
        return new IpcRunScriptInvokedResponse(
            new IpcRunScriptReturnValueOutcome(returnValueJson));
    }

    public static IpcRunScriptResponse NotInvoked(
        string reason,
        IpcRunScriptNotInvokedDetails? details = null)
    {
        return new IpcRunScriptNotInvokedResponse(reason, details);
    }

    public static IpcRunScriptResponse InvokedWithException(string message)
    {
        return new IpcRunScriptInvokedResponse(
            new IpcRunScriptExceptionOutcome(message));
    }

    private protected IpcRunScriptResponse()
    {
    }
}

[MessagePackObject]
public sealed class IpcRunScriptNotInvokedResponse(
    string reason,
    IpcRunScriptNotInvokedDetails? details = null) : IpcRunScriptResponse
{
    [Key(0)]
    public string Reason { get; } =
        reason ?? throw new ArgumentNullException(nameof(reason));

    [Key(1)]
    public IpcRunScriptNotInvokedDetails? Details { get; } = details;
}

[MessagePackObject]
public sealed class IpcRunScriptNotInvokedDetails(
    IReadOnlyList<string>? referenceDiagnostics,
    IReadOnlyList<string>? compilationDiagnostics)
{
    private static readonly IReadOnlyList<string> EmptyList =
        new ReadOnlyCollection<string>([]);

    [Key(0)]
    public IReadOnlyList<string> ReferenceDiagnostics { get; } =
        NormalizeList(referenceDiagnostics);

    [Key(1)]
    public IReadOnlyList<string> CompilationDiagnostics { get; } =
        NormalizeList(compilationDiagnostics);

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string>? values)
    {
        return values is { Count: > 0 }
            ? new ReadOnlyCollection<string>([.. values])
            : EmptyList;
    }
}

[MessagePackObject]
public sealed class IpcRunScriptInvokedResponse(
    IpcRunScriptInvocationOutcome outcome) : IpcRunScriptResponse
{
    [Key(0)]
    public IpcRunScriptInvocationOutcome Outcome { get; } =
        outcome ?? throw new ArgumentNullException(nameof(outcome));
}

[Union(0, typeof(IpcRunScriptReturnValueOutcome))]
[Union(1, typeof(IpcRunScriptExceptionOutcome))]
[MessagePackObject]
public abstract class IpcRunScriptInvocationOutcome
{
    private protected IpcRunScriptInvocationOutcome()
    {
    }
}

[MessagePackObject]
public sealed class IpcRunScriptReturnValueOutcome(
    string returnValueJson) : IpcRunScriptInvocationOutcome
{
    [Key(0)]
    public string ReturnValueJson { get; } =
        returnValueJson ?? throw new ArgumentNullException(nameof(returnValueJson));
}

[MessagePackObject]
public sealed class IpcRunScriptExceptionOutcome(
    string message) : IpcRunScriptInvocationOutcome
{
    [Key(0)]
    public string Message { get; } =
        message ?? throw new ArgumentNullException(nameof(message));
}
