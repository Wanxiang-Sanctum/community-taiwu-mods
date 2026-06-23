using Cysharp.Threading.Tasks;
using Wanxiang.Xiangshu.Ipc;
using Wanxiang.Xiangshu.Scripting;

namespace Wanxiang.Xiangshu.Frontend.ScriptHost;

internal sealed class FrontendScriptEntryDispatcher : IScriptEntryDispatcher
{
    public async Task<object?> InvokeAsync(
        Func<object?> invokeEntry,
        IpcScriptEntryThread entryThread,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (entryThread)
        {
            case IpcScriptEntryThread.Current:
                return invokeEntry();

            case IpcScriptEntryThread.MainThread:
                await UniTask.SwitchToMainThread(cancellationToken);
                return invokeEntry();

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(entryThread),
                    entryThread,
                    "Unsupported script entry thread.");
        }
    }
}
