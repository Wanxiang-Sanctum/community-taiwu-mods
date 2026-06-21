namespace Wanxiang.Taiwu.ModRpc;

internal sealed class HandlerRegistration(Action dispose) : IDisposable
{
    private Action? _dispose = CreateDispose(dispose);

    public void Dispose()
    {
        Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }

    private static Action CreateDispose(Action dispose)
    {
        Guard.ThrowIfNull(dispose, nameof(dispose));
        return dispose;
    }
}
