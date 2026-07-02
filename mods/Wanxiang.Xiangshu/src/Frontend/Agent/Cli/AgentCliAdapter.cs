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
    private static readonly Dictionary<AgentAdapter, IAgentCliAdapter> Adapters = CreateAdapters();

    public static IAgentCliAdapter Get(AgentAdapter adapter)
    {
        return Adapters.TryGetValue(adapter, out IAgentCliAdapter? cliAdapter)
            ? cliAdapter
            : throw new ArgumentOutOfRangeException(nameof(adapter), adapter, null);
    }

    private static Dictionary<AgentAdapter, IAgentCliAdapter> CreateAdapters()
    {
        Dictionary<AgentAdapter, IAgentCliAdapter> adapters = new()
        {
            [AgentAdapter.Codex] = new CodexCliAdapter(),
            [AgentAdapter.CodeBuddy] = new CodeBuddyCliAdapter(),
        };

        foreach (AgentAdapterDefinition definition in AgentAdapterCatalog.All)
        {
            if (!adapters.ContainsKey(definition.Adapter))
            {
                throw new InvalidOperationException(
                    "No CLI adapter is registered for " + definition.Key + ".");
            }
        }

        return adapters;
    }
}
