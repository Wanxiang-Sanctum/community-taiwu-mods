using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Wanxiang.Xiangshu.Frontend.Agent.Cli;

internal abstract class PrintModeStreamJsonCliAdapter : IAgentCliAdapter
{
    public bool RedirectStandardInput => true;

    public void ConfigureStartInfo(
        ProcessStartInfo startInfo,
        AgentCliInvocation invocation)
    {
        string mcpConfigPath = invocation.TempFiles.WriteHttpMcpConfig(
            invocation.McpServerUrl,
            invocation.BearerToken);

        startInfo.ArgumentList.Add("--print");
        if (!string.IsNullOrWhiteSpace(invocation.AgentSessionId))
        {
            startInfo.ArgumentList.Add("--resume");
            startInfo.ArgumentList.Add(invocation.AgentSessionId);
        }

        startInfo.ArgumentList.Add("--output-format");
        startInfo.ArgumentList.Add("stream-json");
        startInfo.ArgumentList.Add("--verbose");
        startInfo.ArgumentList.Add("--dangerously-skip-permissions");
        startInfo.ArgumentList.Add("--mcp-config");
        startInfo.ArgumentList.Add(mcpConfigPath);
        if (invocation.RequireChatReplySchema)
        {
            startInfo.ArgumentList.Add("--json-schema");
            startInfo.ArgumentList.Add(AgentCliChatReplySchema.CreateCompactJson());
        }
    }

    public bool TryExtractAssistantMessage(
        AgentProcessResult result,
        [NotNullWhen(true)]
        out string? assistantMessage)
    {
        return AgentCliJson.TryExtractStreamJsonChatReply(result.Stdout, out assistantMessage);
    }

    public string? ExtractAgentSessionId(AgentProcessResult result)
    {
        return AgentCliJson.ExtractStreamJsonSessionId(result.Stdout);
    }
}

internal sealed class ClaudeCliAdapter : PrintModeStreamJsonCliAdapter;

internal sealed class CodeBuddyCliAdapter : PrintModeStreamJsonCliAdapter;
