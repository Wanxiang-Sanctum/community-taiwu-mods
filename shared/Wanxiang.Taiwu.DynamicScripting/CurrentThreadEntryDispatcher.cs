namespace Wanxiang.Taiwu.DynamicScripting;

internal sealed class CurrentThreadEntryDispatcher : IDynamicScriptEntryDispatcher
{
    public static CurrentThreadEntryDispatcher Instance { get; } = new();

    private CurrentThreadEntryDispatcher()
    {
    }

    public Task<object?> InvokeAsync(
        Func<object?> invokeEntry,
        DynamicScriptEntryThread entryThread,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return entryThread switch
        {
            DynamicScriptEntryThread.Current => Task.FromResult(invokeEntry()),
            DynamicScriptEntryThread.MainThread => throw new InvalidOperationException(
                "mainThread script execution is not available without a host dispatcher."),
            _ => throw new ArgumentOutOfRangeException(
                nameof(entryThread),
                entryThread,
                "Unsupported script entry thread."),
        };
    }
}
