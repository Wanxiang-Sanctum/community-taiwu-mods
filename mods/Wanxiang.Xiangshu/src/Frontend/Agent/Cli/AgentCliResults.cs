using System.Diagnostics.CodeAnalysis;

namespace Wanxiang.Xiangshu.Frontend.Agent.Cli;

internal sealed class AgentCliChatResult(
    string assistantMessage,
    string? agentSessionId)
{
    public string AssistantMessage { get; } = assistantMessage;

    public string? AgentSessionId { get; } = agentSessionId;
}

[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "This internal exception carries structured CLI failure facts and is only constructed with that context.")]
[SuppressMessage(
    "Roslynator",
    "RCS1194:Implement exception constructors",
    Justification = "This internal exception carries structured CLI failure facts and is only constructed with that context.")]
internal sealed class AgentCliFailureException(
    string reason,
    string message,
    int? exitCode,
    string? stderrExcerpt) : InvalidOperationException(message)
{
    public string Reason { get; } = reason;

    public int? ExitCode { get; } = exitCode;

    public string? StderrExcerpt { get; } = stderrExcerpt;
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
