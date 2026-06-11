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
            schema: "wanxiang.xiangshu.chat-turn.v1",
            session: new AgentChatTurnSession(
                turn.SessionId,
                turn.BatchId,
                turn.ExternalSessionId),
            participants: new AgentChatTurnParticipants(
                player: turn.PlayerName,
                assistant: turn.AssistantName),
            currentPlayerBatch: turn.PlayerMessages,
            requestedOutput: new AgentChatTurnRequestedOutput(
                kind: "assistant-message",
                speaker: turn.AssistantName,
                respondsTo: "latest-player-message"));

        return JsonConvert.SerializeObject(input, Formatting.Indented, JsonSettings);
    }
}

internal sealed class AgentChatTurnInput(
    string schema,
    AgentChatTurnSession session,
    AgentChatTurnParticipants participants,
    IReadOnlyList<string> currentPlayerBatch,
    AgentChatTurnRequestedOutput requestedOutput)
{
    [JsonProperty("schema")]
    public string Schema { get; } = schema;

    [JsonProperty("session")]
    public AgentChatTurnSession Session { get; } = session;

    [JsonProperty("participants")]
    public AgentChatTurnParticipants Participants { get; } = participants;

    [JsonProperty("currentPlayerBatch")]
    public IReadOnlyList<string> CurrentPlayerBatch { get; } = currentPlayerBatch;

    [JsonProperty("requestedOutput")]
    public AgentChatTurnRequestedOutput RequestedOutput { get; } = requestedOutput;
}

internal sealed class AgentChatTurnSession(
    string internalSessionId,
    string batchId,
    string? externalAgentSessionId)
{
    [JsonProperty("internalSessionId")]
    public string InternalSessionId { get; } = internalSessionId;

    [JsonProperty("batchId")]
    public string BatchId { get; } = batchId;

    [JsonProperty("externalAgentSessionId")]
    public string? ExternalAgentSessionId { get; } = externalAgentSessionId;
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

internal sealed class AgentChatTurnRequestedOutput(
    string kind,
    string speaker,
    string respondsTo)
{
    [JsonProperty("kind")]
    public string Kind { get; } = kind;

    [JsonProperty("speaker")]
    public string Speaker { get; } = speaker;

    [JsonProperty("respondsTo")]
    public string RespondsTo { get; } = respondsTo;
}
