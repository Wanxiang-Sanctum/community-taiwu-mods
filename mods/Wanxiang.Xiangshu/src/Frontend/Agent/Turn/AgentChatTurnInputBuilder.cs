using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Wanxiang.Xiangshu.Frontend.Agent.Turn;

internal static class AgentChatTurnInputBuilder
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Converters =
        {
            new StringEnumConverter { AllowIntegerValues = false },
        },
        TypeNameHandling = TypeNameHandling.None,
    };

    public static string Build(AgentChatTurn turn)
    {
        AgentChatTurnInput input = new(
            playerName: turn.PlayerName,
            turnMessages: turn.Messages);

        return JsonConvert.SerializeObject(input, Formatting.Indented, JsonSettings);
    }
}

internal sealed class AgentChatTurnInput(
    string playerName,
    IReadOnlyList<AgentChatTurnMessage> turnMessages)
{
    [JsonProperty("playerName")]
    public string PlayerName { get; } = playerName;

    [JsonProperty("turnMessages")]
    public IReadOnlyList<AgentChatTurnMessage> TurnMessages { get; } = turnMessages;
}
