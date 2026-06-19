using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wanxiang.Xiangshu.Frontend.Agent;

namespace Wanxiang.Xiangshu.Frontend.Agent.Cli;

internal interface IAgentCliAdapter
{
    bool RedirectStandardInput { get; }

    void ConfigureStartInfo(
        ProcessStartInfo startInfo,
        AgentCliInvocation invocation);

    bool HasExplicitErrorResult(AgentProcessResult result);

    bool TryExtractAssistantMessage(
        AgentProcessResult result,
        [NotNullWhen(true)]
        out string? assistantMessage);

    string? ExtractAgentSessionId(AgentProcessResult result);
}

internal sealed class AgentCliInvocation(
    AgentSettings settings,
    string mcpServerUrl,
    AgentCliTempFiles tempFiles,
    string turnInputJson,
    string? agentSessionId,
    bool requireChatReplySchema,
    Mcp.McpBearerToken bearerToken)
{
    public AgentSettings Settings { get; } = settings;

    public string McpServerUrl { get; } = mcpServerUrl;

    public AgentCliTempFiles TempFiles { get; } = tempFiles;

    public string TurnInputJson { get; } = turnInputJson;

    public string? AgentSessionId { get; } = agentSessionId;

    public bool RequireChatReplySchema { get; } = requireChatReplySchema;

    public Mcp.McpBearerToken BearerToken { get; } = bearerToken;
}

internal static class AgentCliAdapters
{
    private static readonly IAgentCliAdapter CodexAdapter = new CodexCliAdapter();
    private static readonly IAgentCliAdapter ClaudeAdapter = new ClaudeCliAdapter();
    private static readonly IAgentCliAdapter CodeBuddyAdapter = new CodeBuddyCliAdapter();

    public static IAgentCliAdapter Get(AgentAdapter adapter)
    {
        return adapter switch
        {
            AgentAdapter.Codex => CodexAdapter,
            AgentAdapter.Claude => ClaudeAdapter,
            AgentAdapter.CodeBuddy => CodeBuddyAdapter,
            _ => throw new ArgumentOutOfRangeException(nameof(adapter), adapter, null),
        };
    }
}
