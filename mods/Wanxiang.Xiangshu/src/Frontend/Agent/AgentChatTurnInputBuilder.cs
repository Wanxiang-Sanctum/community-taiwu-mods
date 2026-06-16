using Newtonsoft.Json;

namespace Wanxiang.Xiangshu.Frontend.Agent;

internal static class AgentChatTurnInputBuilder
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
    };

    public static string Build(AgentChatTurn turn)
    {
        AgentChatTurnInput input = new(
            participants: new AgentChatTurnParticipants(
                player: turn.PlayerName,
                assistant: turn.AssistantName),
            currentPlayerMessages: turn.PlayerMessages);

        return JsonConvert.SerializeObject(input, Formatting.Indented, JsonSettings);
    }
}

internal sealed class AgentChatTurnInput(
    AgentChatTurnParticipants participants,
    IReadOnlyList<string> currentPlayerMessages)
{
    [JsonProperty("participants")]
    public AgentChatTurnParticipants Participants { get; } = participants;

    [JsonProperty("currentPlayerMessages")]
    public IReadOnlyList<string> CurrentPlayerMessages { get; } = currentPlayerMessages;
}

internal sealed class AgentChatTurnParticipants(
    string player,
    string assistant)
{
    [JsonProperty("player")]
    public string Player { get; } = player;

    [JsonProperty("assistant")]
    public string Assistant { get; } = assistant;
}
