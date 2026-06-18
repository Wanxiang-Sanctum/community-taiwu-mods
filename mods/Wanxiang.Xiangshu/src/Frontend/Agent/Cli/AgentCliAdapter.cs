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
    private static readonly IAgentCliAdapter Codex = new CodexCliAdapter();
    private static readonly IAgentCliAdapter Claude = new ClaudeCliAdapter();
    private static readonly IAgentCliAdapter CodeBuddy = new CodeBuddyCliAdapter();

    public static IAgentCliAdapter Get(AgentAdapter adapter)
    {
        return (int)adapter switch
        {
            (int)AgentAdapter.Claude => Claude,
            (int)AgentAdapter.CodeBuddy => CodeBuddy,
            _ => Codex,
        };
    }
}
