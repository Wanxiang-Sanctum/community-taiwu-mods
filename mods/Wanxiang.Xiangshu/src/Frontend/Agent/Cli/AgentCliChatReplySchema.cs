using Newtonsoft.Json;

namespace Wanxiang.Xiangshu.Frontend.Agent.Cli;

internal static class AgentCliChatReplySchema
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
    };

    public static string CreateCompactJson()
    {
        return Serialize(Formatting.None);
    }

    public static string CreateIndentedJson()
    {
        return Serialize(Formatting.Indented);
    }

    private static string Serialize(Formatting formatting)
    {
        return JsonConvert.SerializeObject(CreateDocument(), formatting, JsonSettings);
    }

    private static object CreateDocument()
    {
        return new
        {
            type = "object",
            properties = new
            {
                reply = new
                {
                    type = "string",
                    minLength = 1,
                },
            },
            required = new[] { "reply" },
            additionalProperties = false,
        };
    }
}
