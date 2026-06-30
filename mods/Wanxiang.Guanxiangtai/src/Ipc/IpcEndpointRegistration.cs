namespace Wanxiang.Guanxiangtai.Ipc;

public sealed class IpcEndpointRegistration(
    string manifestPath,
    IpcEndpoint endpoint) : IDisposable
{
    private readonly string _manifestPath = manifestPath;
    private readonly IpcEndpoint _endpoint = endpoint;
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        IpcEndpointRegistry.Unregister(_manifestPath, _endpoint);
    }
}
