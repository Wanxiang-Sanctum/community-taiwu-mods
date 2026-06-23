using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Scripting;

public interface IScriptEntryDispatcher
{
    Task<object?> InvokeAsync(
        Func<object?> invokeEntry,
        IpcScriptEntryThread entryThread,
        CancellationToken cancellationToken);
}

internal sealed class CurrentThreadEntryDispatcher : IScriptEntryDispatcher
{
    public static CurrentThreadEntryDispatcher Instance { get; } = new();

    private CurrentThreadEntryDispatcher()
    {
    }

    public Task<object?> InvokeAsync(
        Func<object?> invokeEntry,
        IpcScriptEntryThread entryThread,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return entryThread switch
        {
            IpcScriptEntryThread.Current => Task.FromResult(invokeEntry()),
            IpcScriptEntryThread.MainThread => throw new InvalidOperationException(
                "mainThread script execution is not available for this target side."),
            _ => throw new ArgumentOutOfRangeException(
                nameof(entryThread),
                entryThread,
                "Unsupported script entry thread."),
        };
    }
}
