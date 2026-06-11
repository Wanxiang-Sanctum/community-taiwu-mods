using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Frontend.Agent;

internal sealed class AgentCliTempFiles : IDisposable
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public const string ChatReplySchemaJson =
        """
        {
          "type": "object",
          "properties": {
            "reply": {
              "type": "string",
              "minLength": 1
            }
          },
          "required": ["reply"],
          "additionalProperties": false
        }
        """;

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

    public string WriteClaudeMcpConfig(string mcpUrl)
    {
        JObject config = new(
            new JProperty(
                "mcpServers",
                new JObject(
                    new JProperty(
                        "xiangshu",
                        new JObject(
                            new JProperty("type", "http"),
                            new JProperty("url", mcpUrl))))));
        File.WriteAllText(
            McpConfigPath,
            config.ToString(Formatting.Indented, []),
            Utf8NoBom);
        return McpConfigPath;
    }

    public string WriteChatReplySchema()
    {
        File.WriteAllText(ChatReplySchemaPath, ChatReplySchemaJson, Utf8NoBom);
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
