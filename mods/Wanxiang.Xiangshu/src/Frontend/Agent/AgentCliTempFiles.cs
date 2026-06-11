using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Wanxiang.Xiangshu.Frontend.Agent;

internal sealed class AgentCliTempFiles : IDisposable
{
    private readonly string _directory;

    private AgentCliTempFiles(string directory)
    {
        _directory = directory;
        LastMessagePath = Path.Combine(directory, "last-message.txt");
        McpConfigPath = Path.Combine(directory, "mcp.json");
    }

    public string LastMessagePath { get; }

    public string McpConfigPath { get; }

    public static AgentCliTempFiles Create()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "Wanxiang.Xiangshu",
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
            config.ToString(Formatting.Indented),
            Encoding.UTF8);
        return McpConfigPath;
    }

    public string ReadLastMessage()
    {
        return File.Exists(LastMessagePath)
            ? File.ReadAllText(LastMessagePath, Encoding.UTF8)
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
