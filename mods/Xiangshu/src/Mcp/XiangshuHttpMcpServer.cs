using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Xiangshu.Mcp;

public sealed class XiangshuHttpMcpServer : IDisposable
{
    private const string SessionIdHeaderName = "Mcp-Session-Id";

    private readonly CancellationTokenSource _stopping = new();
    private readonly HttpListener _listener;
    private readonly StreamableHttpServerTransport _transport;
    private readonly McpServer _server;
    private readonly Task _listenerTask;
    private readonly Task _serverTask;
    private readonly XiangshuMcpEndpointRegistration _registration;

    private XiangshuHttpMcpServer(
        XiangshuMcpServerStartOptions options,
        int port,
        string accessToken)
    {
        Endpoint = new Uri($"http://127.0.0.1:{port}/mcp/");
        AccessToken = accessToken;

        HttpListener listener = new();
        StreamableHttpServerTransport? transport = null;
        McpServer? server = null;
        XiangshuMcpEndpointRegistration? registration = null;

        try
        {
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();

            transport = new StreamableHttpServerTransport(NullLoggerFactory.Instance)
            {
                Stateless = false,
            };
            server = McpServer.Create(
                transport,
                CreateServerOptions(options),
                NullLoggerFactory.Instance,
                serviceProvider: null);

            registration = XiangshuMcpEndpointRegistry.Register(
                new XiangshuMcpEndpoint
                {
                    Side = options.Side,
                    ServerName = options.ServerName,
                    ServerTitle = options.ServerTitle,
                    ServerVersion = options.ServerVersion,
                    Transport = "streamable-http",
                    Url = Endpoint.ToString(),
                    AuthorizationHeader = $"Bearer {accessToken}",
                    ProcessId = Process.GetCurrentProcess().Id,
                    StartedAtUtc = DateTimeOffset.UtcNow,
                });
        }
        catch
        {
            registration?.Dispose();
            server?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            transport?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            listener.Close();
            _stopping.Dispose();
            throw;
        }

        _listener = listener;
        _transport = transport;
        _server = server;
        _registration = registration;
        _serverTask = Task.Run(() => _server.RunAsync(_stopping.Token));
        _listenerTask = Task.Run(() => ListenAsync(_stopping.Token));
    }

    public Uri Endpoint { get; }

    public string AccessToken { get; }

    public static XiangshuHttpMcpServer Start(XiangshuMcpServerStartOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        int port = ReserveLoopbackPort();
        string accessToken = CreateAccessToken();

        return new XiangshuHttpMcpServer(options, port, accessToken);
    }

    public void Dispose()
    {
        try
        {
            _registration.Dispose();
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        _stopping.Cancel();

        try
        {
            _listener.Abort();
            _listenerTask.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (HttpListenerException)
        {
        }

        try
        {
            _transport.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _serverTask.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _server.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _listener.Close();
            _stopping.Dispose();
        }
    }

    private static McpServerOptions CreateServerOptions(XiangshuMcpServerStartOptions options)
    {
        McpServerPrimitiveCollection<McpServerTool> tools = new(StringComparer.Ordinal);

        foreach (Type toolType in options.ToolTypes)
        {
            foreach ((MethodInfo method, XiangshuMcpToolAttribute attribute) in FindToolMethods(toolType))
            {
                if (!method.IsStatic)
                {
                    throw new InvalidOperationException(
                        $"MCP tool method '{toolType.FullName}.{method.Name}' must be static.");
                }

                tools.Add(
                    McpServerTool.Create(
                        method,
                        target: null,
                        options: new McpServerToolCreateOptions
                        {
                            Name = attribute.Name,
                            Title = attribute.Title,
                        }));
            }
        }

        return new McpServerOptions
        {
            ServerInfo = new Implementation
            {
                Name = options.ServerName,
                Title = options.ServerTitle,
                Version = options.ServerVersion,
            },
            ServerInstructions = options.ServerInstructions,
            Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability(),
            },
            ToolCollection = tools,
        };
    }

    private static IEnumerable<(MethodInfo Method, XiangshuMcpToolAttribute Attribute)> FindToolMethods(Type toolType)
    {
        const BindingFlags Flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        foreach (MethodInfo method in toolType.GetMethods(Flags))
        {
            XiangshuMcpToolAttribute? attribute = method.GetCustomAttribute<XiangshuMcpToolAttribute>();

            if (attribute is not null)
            {
                yield return (method, attribute);
            }
        }
    }

    private static int ReserveLoopbackPort()
    {
        TcpListener socket = new(IPAddress.Loopback, port: 0);

        try
        {
            socket.Start();
            return ((IPEndPoint)socket.LocalEndpoint).Port;
        }
        finally
        {
            socket.Stop();
        }
    }

    private static string CreateAccessToken()
    {
        byte[] bytes = new byte[32];

        using (RandomNumberGenerator random = RandomNumberGenerator.Create())
        {
            random.GetBytes(bytes);
        }

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;

            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            _ = Task.Run(() => HandleRequestAsync(context, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsAuthorized(context.Request))
            {
                await WriteTextResponseAsync(context.Response, HttpStatusCode.Unauthorized, "Unauthorized", cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            if (!IsMcpEndpoint(context.Request))
            {
                await WriteTextResponseAsync(context.Response, HttpStatusCode.NotFound, "Not Found", cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            if (string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await HandleGetRequestAsync(context.Response, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                await HandlePostRequestAsync(context.Request, context.Response, cancellationToken).ConfigureAwait(false);
                return;
            }

            await WriteTextResponseAsync(
                    context.Response,
                    HttpStatusCode.MethodNotAllowed,
                    "Method Not Allowed",
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            await WriteTextResponseAsync(context.Response, HttpStatusCode.BadRequest, ex.Message, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await WriteTextResponseAsync(
                    context.Response,
                    HttpStatusCode.InternalServerError,
                    "MCP endpoint error: " + ex.Message,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
    }

    private bool IsAuthorized(HttpListenerRequest request)
    {
        if (request.RemoteEndPoint is null || !IPAddress.IsLoopback(request.RemoteEndPoint.Address))
        {
            return false;
        }

        return string.Equals(
            request.Headers["Authorization"],
            "Bearer " + AccessToken,
            StringComparison.Ordinal);
    }

    private static bool IsMcpEndpoint(HttpListenerRequest request)
    {
        string path = request.Url?.AbsolutePath ?? string.Empty;

        return string.Equals(path, "/mcp/", StringComparison.Ordinal);
    }

    private async Task HandleGetRequestAsync(HttpListenerResponse response, CancellationToken cancellationToken)
    {
        ApplySessionHeader(response);
        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = "text/event-stream";
        response.SendChunked = true;

        await _transport.HandleGetRequestAsync(response.OutputStream, cancellationToken).ConfigureAwait(false);
        response.Close();
    }

    private async Task HandlePostRequestAsync(
        HttpListenerRequest request,
        HttpListenerResponse response,
        CancellationToken cancellationToken)
    {
        JsonRpcMessage? message = await JsonSerializer
            .DeserializeAsync<JsonRpcMessage>(request.InputStream, McpJsonUtilities.DefaultOptions, cancellationToken)
            .ConfigureAwait(false);

        if (message is null)
        {
            await WriteTextResponseAsync(response, HttpStatusCode.BadRequest, "Missing JSON-RPC message", cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        MemoryStream responseBody = new();

        try
        {
            bool wroteResponse = await _transport
                .HandlePostRequestAsync(message, responseBody, cancellationToken)
                .ConfigureAwait(false);

            ApplySessionHeader(response);

            if (!wroteResponse)
            {
                response.StatusCode = (int)HttpStatusCode.Accepted;
                response.Close();
                return;
            }

            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "text/event-stream";
            response.ContentLength64 = responseBody.Length;

            responseBody.Position = 0;
            await responseBody.CopyToAsync(response.OutputStream, bufferSize: 81920, cancellationToken)
                .ConfigureAwait(false);
            response.Close();
        }
        finally
        {
            await responseBody.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void ApplySessionHeader(HttpListenerResponse response)
    {
        if (!string.IsNullOrEmpty(_transport.SessionId))
        {
            response.Headers[SessionIdHeaderName] = _transport.SessionId;
        }
    }

    private static async Task WriteTextResponseAsync(
        HttpListenerResponse response,
        HttpStatusCode statusCode,
        string text,
        CancellationToken cancellationToken)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);

        response.StatusCode = (int)statusCode;
        response.ContentType = "text/plain; charset=utf-8";
        response.ContentLength64 = bytes.Length;

        await response.OutputStream.WriteAsync(bytes.AsMemory(0, bytes.Length), cancellationToken).ConfigureAwait(false);
        response.Close();
    }
}

public sealed class XiangshuMcpServerStartOptions
{
    public string Side { get; set; } = string.Empty;

    public string ServerName { get; set; } = string.Empty;

    public string ServerTitle { get; set; } = string.Empty;

    public string ServerVersion { get; set; } = string.Empty;

    public string ServerInstructions { get; set; } = string.Empty;

    public IReadOnlyList<Type> ToolTypes { get; set; } = [];
}
