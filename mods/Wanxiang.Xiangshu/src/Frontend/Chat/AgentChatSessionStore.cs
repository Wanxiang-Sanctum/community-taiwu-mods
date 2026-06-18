using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Frontend.Chat;

internal sealed class AgentChatSessionStore(string workingDirectory)
{
    private const string MessageIdPrefix = "message-";

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters =
        {
            new StringEnumConverter { AllowIntegerValues = false },
        },
        NullValueHandling = NullValueHandling.Ignore,
    };

    private readonly string _currentPath = XiangshuRuntimePaths.GetCurrentChatSessionPath(workingDirectory);
    private readonly string _sessionsDirectory = XiangshuRuntimePaths.GetChatSessionSnapshotsDirectory(workingDirectory);

    public AgentChatSessionState? LoadCurrent()
    {
        EnsureDirectories();

        if (!File.Exists(_currentPath))
        {
            return null;
        }

        PersistedCurrentChatSession current = ReadJson<PersistedCurrentChatSession>(_currentPath);

        string sessionId = NormalizeSessionId(current.CurrentSessionId, "currentSessionId", _currentPath);
        string sessionPath = GetSessionPath(sessionId);
        PersistedChatSession session = ReadJson<PersistedChatSession>(sessionPath);
        return CreateState(session, sessionPath);
    }

    public void Save(AgentChatSessionState state)
    {
        EnsureDirectories();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        WriteJson(GetSessionPath(state.SessionId), CreatePersistedSession(state, now));
        WriteJson(
            _currentPath,
            new PersistedCurrentChatSession
            {
                UpdatedAt = now,
                CurrentSessionId = state.SessionId,
            });
    }

    public void DeleteSnapshot(string sessionId)
    {
        EnsureDirectories();

        string normalizedSessionId = NormalizeSessionId(sessionId, "sessionId", _sessionsDirectory);
        string sessionPath = GetSessionPath(normalizedSessionId);

        if (File.Exists(sessionPath))
        {
            File.Delete(sessionPath);
        }
    }

    private static AgentChatSessionState CreateState(
        PersistedChatSession session,
        string sessionPath)
    {
        string sessionId = NormalizeSessionId(session.SessionId, "sessionId", sessionPath);

        List<AgentChatMessage> messages = [];
        int highestMessageOrdinal = 0;

        List<PersistedChatMessage> visibleMessages = session.VisibleMessages
            ?? throw new InvalidDataException($"Missing chat session field 'visibleMessages' in {sessionPath}.");

        foreach (PersistedChatMessage persistedMessage in visibleMessages)
        {
            AgentChatMessage message = CreateMessage(persistedMessage, sessionPath);
            messages.Add(message);
            highestMessageOrdinal = Math.Max(
                highestMessageOrdinal,
                ParseMessageOrdinal(message.Id, sessionPath));
        }

        int lastMessageOrdinal = session.LastMessageOrdinal
            ?? throw new InvalidDataException($"Missing chat session field 'lastMessageOrdinal' in {sessionPath}.");
        ValidateLastMessageOrdinal(lastMessageOrdinal, highestMessageOrdinal, sessionPath);

        return new AgentChatSessionState(
            sessionId,
            NormalizeRequired(session.Adapter, "adapter", sessionPath),
            NormalizeNullable(session.AgentSessionId),
            session.RequiresReset,
            lastMessageOrdinal,
            messages);
    }

    private static PersistedChatSession CreatePersistedSession(
        AgentChatSessionState state,
        DateTimeOffset now)
    {
        return new PersistedChatSession
        {
            UpdatedAt = now,
            SessionId = state.SessionId,
            Adapter = state.Adapter,
            AgentSessionId = state.AgentSessionId,
            RequiresReset = state.RequiresReset,
            LastMessageOrdinal = state.LastMessageOrdinal,
            VisibleMessages =
            [
                .. state.VisibleMessages.Select(CreatePersistedMessage),
            ],
        };
    }

    private static PersistedChatMessage CreatePersistedMessage(AgentChatMessage message)
    {
        return new PersistedChatMessage
        {
            Id = message.Id,
            CreatedAt = message.CreatedAt.ToUniversalTime(),
            Role = message.Role,
            SpeakerName = message.SpeakerName,
            Content = message.Content,
            Origin = message.Origin,
        };
    }

    private static AgentChatMessage CreateMessage(
        PersistedChatMessage persistedMessage,
        string sessionPath)
    {
        return new AgentChatMessage(
            NormalizeRequired(persistedMessage.Id, "message.id", sessionPath),
            NormalizeRequiredTimestamp(persistedMessage.CreatedAt, "message.createdAt", sessionPath),
            NormalizeRequiredEnum(persistedMessage.Role, "message.role", sessionPath),
            NormalizeRequired(persistedMessage.SpeakerName, "message.speakerName", sessionPath),
            NormalizeRequired(persistedMessage.Content, "message.content", sessionPath),
            NormalizeRequiredEnum(persistedMessage.Origin, "message.origin", sessionPath));
    }

    private string GetSessionPath(string sessionId)
    {
        return Path.Combine(_sessionsDirectory, sessionId + ".json");
    }

    private void EnsureDirectories()
    {
        _ = Directory.CreateDirectory(_sessionsDirectory);

        string? currentDirectory = Path.GetDirectoryName(_currentPath);

        if (!string.IsNullOrEmpty(currentDirectory))
        {
            _ = Directory.CreateDirectory(currentDirectory);
        }
    }

    private static T ReadJson<T>(string path)
        where T : class
    {
        string json = File.ReadAllText(path, Utf8NoBom);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException($"Chat session file is empty: {path}");
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(json, JsonSettings)
                ?? throw new InvalidDataException($"Chat session file is not a JSON object: {path}");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Invalid chat session JSON in {path}.", ex);
        }
    }

    private static void WriteJson(
        string path,
        object value)
    {
        string json = JsonConvert.SerializeObject(value, Formatting.Indented, JsonSettings);
        File.WriteAllText(path, json + Environment.NewLine, Utf8NoBom);
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeNullable(string? value)
    {
        string normalized = Normalize(value);
        return normalized.Length == 0 ? null : normalized;
    }

    private static int ParseMessageOrdinal(
        string messageId,
        string path)
    {
        if (!messageId.StartsWith(MessageIdPrefix, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Invalid chat message id '{messageId}' in {path}.");
        }

        if (!int.TryParse(
            messageId[MessageIdPrefix.Length..],
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out int parsedOrdinal)
            || parsedOrdinal <= 0)
        {
            throw new InvalidDataException($"Invalid chat message id '{messageId}' in {path}.");
        }

        return parsedOrdinal;
    }

    private static string NormalizeRequired(
        string? value,
        string fieldName,
        string path)
    {
        string normalized = Normalize(value);

        if (normalized.Length == 0)
        {
            throw new InvalidDataException($"Missing chat session field '{fieldName}' in {path}.");
        }

        return normalized;
    }

    private static T NormalizeRequiredEnum<T>(
        T? value,
        string fieldName,
        string path)
        where T : struct, Enum
    {
        if (value is null)
        {
            throw new InvalidDataException($"Missing chat session field '{fieldName}' in {path}.");
        }

        return value.Value;
    }

    private static string NormalizeSessionId(
        string? value,
        string fieldName,
        string path)
    {
        string normalized = NormalizeRequired(value, fieldName, path);

        if (!Guid.TryParseExact(normalized, "N", out Guid sessionId))
        {
            throw new InvalidDataException($"Invalid chat session field '{fieldName}' in {path}.");
        }

        return sessionId.ToString("N");
    }

    private static void ValidateLastMessageOrdinal(
        int lastMessageOrdinal,
        int highestMessageOrdinal,
        string path)
    {
        if (lastMessageOrdinal < 0)
        {
            throw new InvalidDataException(
                $"Invalid chat session field 'lastMessageOrdinal' in {path}.");
        }

        if (lastMessageOrdinal < highestMessageOrdinal)
        {
            throw new InvalidDataException(
                $"Invalid chat session field 'lastMessageOrdinal' in {path}.");
        }
    }

    private static DateTimeOffset NormalizeRequiredTimestamp(
        DateTimeOffset? value,
        string fieldName,
        string path)
    {
        if (value is null || value.Value == default)
        {
            throw new InvalidDataException($"Missing chat session field '{fieldName}' in {path}.");
        }

        return value.Value.ToUniversalTime();
    }

    private sealed class PersistedCurrentChatSession
    {
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public string CurrentSessionId { get; set; } = string.Empty;
    }

    private sealed class PersistedChatSession
    {
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public string SessionId { get; set; } = string.Empty;

        public string? Adapter { get; set; }

        public string? AgentSessionId { get; set; }

        public bool RequiresReset { get; set; }

        public int? LastMessageOrdinal { get; set; }

        public List<PersistedChatMessage>? VisibleMessages { get; set; }
    }

    private sealed class PersistedChatMessage
    {
        public string Id { get; set; } = string.Empty;

        public DateTimeOffset? CreatedAt { get; set; }

        public AgentChatRole? Role { get; set; }

        public string SpeakerName { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public AgentChatMessageOrigin? Origin { get; set; }
    }
}

internal sealed class AgentChatSessionState(
    string sessionId,
    string adapter,
    string? agentSessionId,
    bool requiresReset,
    int lastMessageOrdinal,
    IReadOnlyList<AgentChatMessage> visibleMessages)
{
    public string SessionId { get; } = sessionId;

    public string Adapter { get; } = adapter;

    public string? AgentSessionId { get; } = agentSessionId;

    public bool RequiresReset { get; } = requiresReset;

    public int LastMessageOrdinal { get; } = lastMessageOrdinal;

    public IReadOnlyList<AgentChatMessage> VisibleMessages { get; } = visibleMessages;
}
