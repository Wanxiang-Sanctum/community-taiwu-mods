using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Wanxiang.Xiangshu.Frontend.Agent.Cli;

internal static class AgentCliJson
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
    };

    public static bool TryExtractChatReply(
        string value,
        [NotNullWhen(true)]
        out string? reply)
    {
        reply = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            ChatReplyDocument? document = JsonConvert.DeserializeObject<ChatReplyDocument>(value, JsonSettings);
            return TryNormalizeReply(document?.Reply, out reply);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool TryExtractStreamJsonChatReply(
        string stdout,
        [NotNullWhen(true)]
        out string? reply)
    {
        reply = null;

        if (!TryReadStreamJsonEvents(stdout, out List<StreamJsonEvent>? events))
        {
            return false;
        }

        StreamJsonEvent? resultEvent = null;
        foreach (StreamJsonEvent streamEvent in events)
        {
            if (streamEvent.IsSuccessfulResult())
            {
                resultEvent = streamEvent;
            }
        }

        return resultEvent is not null
            && resultEvent.TryExtractReply(out reply);
    }

    public static bool HasStreamJsonErrorResult(string stdout)
    {
        if (!TryReadStreamJsonEvents(stdout, out List<StreamJsonEvent>? events))
        {
            return false;
        }

        return events.Exists(static streamEvent => streamEvent.IsErrorResult());
    }

    public static string? ExtractStreamJsonSessionId(string stdout)
    {
        if (!TryReadStreamJsonEvents(stdout, out List<StreamJsonEvent>? events))
        {
            return null;
        }

        string? sessionId = null;
        foreach (StreamJsonEvent streamEvent in events)
        {
            if (streamEvent.IsSessionInit())
            {
                sessionId = Normalize(streamEvent.SessionId);
            }
        }

        return sessionId;
    }

    public static string? ExtractCodexThreadId(string stdout)
    {
        string? threadId = null;

        foreach (string line in SplitLines(stdout))
        {
            if (!TryDeserializeLine(line, out CodexJsonEvent? jsonEvent))
            {
                continue;
            }

            threadId = jsonEvent.GetStartedThreadId() ?? threadId;
        }

        return threadId;
    }

    private static bool TryExtractChatReply(
        JToken? token,
        [NotNullWhen(true)]
        out string? reply)
    {
        reply = null;
        if (token is null || token.Type == JTokenType.Null)
        {
            return false;
        }

        if (token.Type == JTokenType.String)
        {
            return TryExtractChatReply(token.Value<string>() ?? string.Empty, out reply);
        }

        ChatReplyDocument? document = TryCreateDocument<ChatReplyDocument>(token);
        return TryNormalizeReply(document?.Reply, out reply);
    }

    private static bool TryNormalizeReply(
        string? value,
        [NotNullWhen(true)]
        out string? reply)
    {
        reply = value?.Trim();
        return !string.IsNullOrWhiteSpace(reply);
    }

    private static bool TryReadStreamJsonEvents(
        string stdout,
        [NotNullWhen(true)]
        out List<StreamJsonEvent>? events)
    {
        events = [];

        foreach (string line in SplitLines(stdout))
        {
            if (!TryDeserializeLine(line, out StreamJsonEvent? streamEvent))
            {
                events = null;
                return false;
            }

            events.Add(streamEvent);
        }

        return events.Count > 0;
    }

    private static IEnumerable<string> SplitLines(string value)
    {
        using StringReader reader = new(value);

        while (reader.ReadLine() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                yield return line;
            }
        }
    }

    private static bool TryDeserializeLine<T>(
        string line,
        [NotNullWhen(true)] out T? document)
        where T : class
    {
        document = null;

        try
        {
            document = JsonConvert.DeserializeObject<T>(line, JsonSettings);
            return document is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static T? TryCreateDocument<T>(JToken token)
        where T : class
    {
        try
        {
            return token.ToObject<T>(JsonSerializer.Create(JsonSettings));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [SuppressMessage(
        "Performance",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by Json.NET during CLI output deserialization.")]
    private sealed class ChatReplyDocument
    {
        [JsonProperty("reply")]
        public string? Reply { get; set; }
    }

    [SuppressMessage(
        "Performance",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by Json.NET during stream-json deserialization.")]
    private sealed class StreamJsonEvent
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("subtype")]
        public string? Subtype { get; set; }

        [JsonProperty("is_error")]
        public bool? IsError { get; set; }

        [JsonProperty("structured_output")]
        public JToken? StructuredOutput { get; set; }

        [JsonProperty("result")]
        public string? Result { get; set; }

        [JsonProperty("session_id")]
        public string? SessionId { get; set; }

        public bool IsSessionInit()
        {
            return string.Equals(Type, "system", StringComparison.Ordinal)
                && string.Equals(Subtype, "init", StringComparison.Ordinal);
        }

        public bool IsErrorResult()
        {
            return string.Equals(Type, "result", StringComparison.Ordinal)
                && IsError == true;
        }

        public bool IsSuccessfulResult()
        {
            return string.Equals(Type, "result", StringComparison.Ordinal)
                && IsError == false;
        }

        public bool TryExtractReply(
            [NotNullWhen(true)]
            out string? reply)
        {
            return AgentCliJson.TryExtractChatReply(StructuredOutput, out reply)
                || AgentCliJson.TryExtractChatReply(Result ?? string.Empty, out reply);
        }
    }

    [SuppressMessage(
        "Performance",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by Json.NET during Codex JSONL deserialization.")]
    private sealed class CodexJsonEvent
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("thread_id")]
        public string? ThreadId { get; set; }

        public string? GetStartedThreadId()
        {
            return string.Equals(Type, "thread.started", StringComparison.Ordinal)
                ? Normalize(ThreadId)
                : null;
        }
    }
}
