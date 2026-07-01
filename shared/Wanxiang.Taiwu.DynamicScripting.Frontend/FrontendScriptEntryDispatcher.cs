using Cysharp.Threading.Tasks;

namespace Wanxiang.Taiwu.DynamicScripting.Frontend;

/// <summary>
/// 将动态脚本入口调用分派到当前前端线程或 Unity 主线程。
/// </summary>
public sealed class FrontendScriptEntryDispatcher : IDynamicScriptEntryDispatcher
{
    /// <inheritdoc />
    public async Task<object?> InvokeAsync(
        Func<object?> invokeEntry,
        DynamicScriptEntryThread entryThread,
        CancellationToken cancellationToken)
    {
        if (invokeEntry is null)
        {
            throw new ArgumentNullException(nameof(invokeEntry));
        }

        cancellationToken.ThrowIfCancellationRequested();

        switch (entryThread)
        {
            case DynamicScriptEntryThread.Current:
                return invokeEntry();

            case DynamicScriptEntryThread.MainThread:
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
