namespace Wanxiang.Guanxiangtai.McpServerRuntime;

public sealed class McpServerEndpointRegistration(
    string endpointFilePath,
    McpServerEndpoint endpoint) : IDisposable
{
    private readonly string _endpointFilePath = endpointFilePath;
    private readonly McpServerEndpoint _endpoint = endpoint;
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        McpServerEndpointRegistry.Unregister(_endpointFilePath, _endpoint);
    }
}
