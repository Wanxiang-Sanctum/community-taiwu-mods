using Wanxiang.Taiwu.DynamicScripting;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Scripting;

public sealed class XiangshuScriptRunner
{
    private const string EntryTypeFullName = "Wanxiang.Xiangshu.Scripting.XiangshuScript";

    private static readonly DynamicScriptEntryContract Contract = new(
        EntryTypeFullName,
        typeof(XiangshuScriptGlobals));

    private readonly ScriptRunnerOptions _options;
    private readonly DynamicScriptRunner _runner;

    public XiangshuScriptRunner(
        string side,
        DynamicScriptReferenceOptions references,
        IDynamicScriptEntryDispatcher? entryDispatcher = null)
        : this(
            new ScriptRunnerOptions(
                side,
                references: references),
            entryDispatcher)
    {
    }

    public XiangshuScriptRunner(
        ScriptRunnerOptions options,
        IDynamicScriptEntryDispatcher? entryDispatcher = null)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(options);
#else
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }
#endif

        _options = options;
        _runner = new DynamicScriptRunner(
            Contract,
            options.References,
            entryDispatcher);
    }

    public async Task<IpcRunScriptResponse> ExecuteAsync(
        IpcRunScriptRequest request,
        CancellationToken cancellationToken = default)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(request);
#else
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }
#endif
        XiangshuScriptGlobals globals = CreateGlobals(
            request.Arguments,
            cancellationToken);

        DynamicScriptRunResult result = await _runner.ExecuteAsync(
            new DynamicScriptRunRequest(
                request.Script,
                ToDynamicEntryThread(request.EntryThread)),
            globals,
            cancellationToken);

        return ToIpcResponse(result);
    }

    private XiangshuScriptGlobals CreateGlobals(
        IReadOnlyDictionary<string, string> arguments,
        CancellationToken cancellationToken)
    {
        return new XiangshuScriptGlobals(
            _options.Side,
            arguments,
            cancellationToken);
    }

    private static DynamicScriptEntryThread ToDynamicEntryThread(
        IpcScriptEntryThread entryThread)
    {
        return entryThread switch
        {
            IpcScriptEntryThread.Current => DynamicScriptEntryThread.Current,
            IpcScriptEntryThread.MainThread => DynamicScriptEntryThread.MainThread,
            _ => throw new ArgumentOutOfRangeException(
                nameof(entryThread),
                entryThread,
                "Unsupported script entry thread."),
        };
    }

    private static IpcRunScriptResponse ToIpcResponse(DynamicScriptRunResult result)
    {
        return result switch
        {
            DynamicScriptNotInvokedResult notInvoked =>
                IpcRunScriptResponse.NotInvoked(
                    notInvoked.Reason,
                    ToIpcDetails(notInvoked.Details)),

            DynamicScriptInvokedResult
            {
                Outcome: DynamicScriptExceptionOutcome exception,
            } =>
                IpcRunScriptResponse.InvokedWithException(exception.Message),

            DynamicScriptInvokedResult
            {
                Outcome: DynamicScriptReturnValueOutcome returnValue,
            } =>
                IpcRunScriptResponse.InvokedWithReturnValue(returnValue.ReturnValueJson),

            _ => throw new InvalidOperationException("Unhandled dynamic script result union case."),
        };
    }

    private static IpcRunScriptNotInvokedDetails? ToIpcDetails(
        DynamicScriptNotInvokedDetails? details)
    {
        if (details is null)
        {
            return null;
        }

        return new IpcRunScriptNotInvokedDetails(
            details.ReferenceDiagnostics,
            details.CompilationDiagnostics);
    }
}
