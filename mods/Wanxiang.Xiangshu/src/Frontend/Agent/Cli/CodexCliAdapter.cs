using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wanxiang.Xiangshu.Ipc;

namespace Wanxiang.Xiangshu.Frontend.Agent.Cli;

internal sealed class CodexCliAdapter : IAgentCliAdapter
{
    public bool RedirectStandardInput => true;

    public void ConfigureStartInfo(
        ProcessStartInfo startInfo,
        AgentCliInvocation invocation)
    {
        string? outputSchemaPath = invocation.RequireChatReplySchema
            ? invocation.TempFiles.WriteChatReplySchema()
            : null;

        startInfo.ArgumentList.Add("exec");
        if (!string.IsNullOrWhiteSpace(invocation.AgentSessionId))
        {
            startInfo.ArgumentList.Add("resume");
        }

        startInfo.ArgumentList.Add("--dangerously-bypass-approvals-and-sandbox");
        startInfo.ArgumentList.Add("--skip-git-repo-check");
        startInfo.ArgumentList.Add("--json");
        startInfo.ArgumentList.Add("--output-last-message");
        startInfo.ArgumentList.Add(invocation.TempFiles.LastMessagePath);
        if (outputSchemaPath is not null)
        {
            startInfo.ArgumentList.Add("--output-schema");
            startInfo.ArgumentList.Add(outputSchemaPath);
        }

        if (string.IsNullOrWhiteSpace(invocation.AgentSessionId))
        {
            startInfo.ArgumentList.Add("--cd");
            startInfo.ArgumentList.Add(invocation.Settings.WorkingDirectory);
        }

        startInfo.ArgumentList.Add("--config");
        startInfo.ArgumentList.Add($"mcp_servers.xiangshu.url=\"{invocation.McpServerUrl}\"");
        startInfo.ArgumentList.Add("--config");
        startInfo.ArgumentList.Add(
            $"mcp_servers.xiangshu.bearer_token_env_var=\"{IpcRuntime.McpBearerTokenEnvironmentVariable}\"");
        if (!string.IsNullOrWhiteSpace(invocation.AgentSessionId))
        {
            startInfo.ArgumentList.Add(invocation.AgentSessionId);
        }

        startInfo.ArgumentList.Add("-");
    }

    public bool TryExtractAssistantMessage(
        AgentProcessResult result,
        [NotNullWhen(true)]
        out string? assistantMessage)
    {
        assistantMessage = null;

        return !string.IsNullOrWhiteSpace(result.LastMessage)
            && AgentCliJson.TryExtractChatReply(result.LastMessage, out assistantMessage);
    }

    public bool HasExplicitErrorResult(AgentProcessResult result)
    {
        _ = result;
        return false;
    }

    public string? ExtractAgentSessionId(AgentProcessResult result)
    {
        return AgentCliJson.ExtractCodexThreadId(result.Stdout);
    }
}
