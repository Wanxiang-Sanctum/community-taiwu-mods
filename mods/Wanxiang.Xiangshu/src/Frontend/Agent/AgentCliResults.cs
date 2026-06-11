namespace Wanxiang.Xiangshu.Frontend.Agent;

internal sealed class AgentCliInvocationResult(
    string assistantMessage,
    string? externalSessionId)
{
    public string AssistantMessage { get; } = assistantMessage;

    public string? ExternalSessionId { get; } = externalSessionId;
}

internal sealed class AgentProcessResult(
    string stdout,
    string stderr,
    int exitCode,
    string lastMessage)
{
    public string Stdout { get; } = stdout;

    public string Stderr { get; } = stderr;

    public int ExitCode { get; } = exitCode;

    public string LastMessage { get; } = lastMessage;
}
