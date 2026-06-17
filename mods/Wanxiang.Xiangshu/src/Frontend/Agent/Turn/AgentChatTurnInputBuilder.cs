using Newtonsoft.Json;

namespace Wanxiang.Xiangshu.Frontend.Agent.Turn;

internal static class AgentChatTurnInputBuilder
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
    };

    public static string Build(AgentChatTurn turn)
    {
        AgentChatTurnInput input = new(
            playerName: turn.PlayerName,
            playerMessages: turn.PlayerMessages);

        return JsonConvert.SerializeObject(input, Formatting.Indented, JsonSettings);
    }
}

internal sealed class AgentChatTurnInput(
    string playerName,
    IReadOnlyList<AgentChatTurnMessage> playerMessages)
{
    [JsonProperty("playerName")]
    public string PlayerName { get; } = playerName;

    [JsonProperty("playerMessages")]
    public IReadOnlyList<AgentChatTurnMessage> PlayerMessages { get; } = playerMessages;
}
