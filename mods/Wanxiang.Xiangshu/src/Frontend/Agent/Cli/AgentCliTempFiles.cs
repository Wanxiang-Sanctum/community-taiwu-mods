using System.Text;
using Newtonsoft.Json;
using Wanxiang.Xiangshu.Frontend.Mcp;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Frontend.Agent.Cli;

internal sealed class AgentCliTempFiles : IDisposable
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
    };

    private readonly string _directory;

    private AgentCliTempFiles(string directory)
    {
        _directory = directory;
        LastMessagePath = Path.Combine(directory, "last-message.txt");
        McpConfigPath = Path.Combine(directory, "mcp.json");
        ChatReplySchemaPath = Path.Combine(directory, "chat-reply.schema.json");
    }

    public string LastMessagePath { get; }

    public string McpConfigPath { get; }

    public string ChatReplySchemaPath { get; }

    public static AgentCliTempFiles Create(string workingDirectory)
    {
        string directory = Path.Combine(
            XiangshuRuntimePaths.GetRuntimeDirectory(workingDirectory),
            "Temp",
            "AgentCli",
            Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(directory);
        return new AgentCliTempFiles(directory);
    }

    public string WriteHttpMcpConfig(
        string mcpServerUrl,
        McpBearerToken bearerToken)
    {
        HttpMcpConfig config = new(
            new Dictionary<string, HttpMcpServerConfig>
            {
                ["xiangshu"] = new(
                    type: "http",
                    url: mcpServerUrl,
                    headers: new Dictionary<string, string>
                    {
                        [IpcRuntime.McpAuthorizationHeaderName] = bearerToken.AuthorizationHeaderValue,
                    }),
            });
        File.WriteAllText(
            McpConfigPath,
            JsonConvert.SerializeObject(config, Formatting.Indented, JsonSettings),
            Utf8NoBom);
        return McpConfigPath;
    }

    public string WriteChatReplySchema()
    {
        File.WriteAllText(ChatReplySchemaPath, AgentCliChatReplySchema.CreateIndentedJson(), Utf8NoBom);
        return ChatReplySchemaPath;
    }

    public string ReadLastMessage()
    {
        return File.Exists(LastMessagePath)
            ? File.ReadAllText(LastMessagePath, Utf8NoBom)
            : string.Empty;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

internal sealed class HttpMcpConfig(
    IReadOnlyDictionary<string, HttpMcpServerConfig> mcpServers)
{
    [JsonProperty("mcpServers")]
    public IReadOnlyDictionary<string, HttpMcpServerConfig> McpServers { get; } = mcpServers;
}

internal sealed class HttpMcpServerConfig(
    string type,
    string url,
    IReadOnlyDictionary<string, string> headers)
{
    [JsonProperty("type")]
    public string Type { get; } = type;

    [JsonProperty("url")]
    public string Url { get; } = url;

    [JsonProperty("headers")]
    public IReadOnlyDictionary<string, string> Headers { get; } = headers;
}
