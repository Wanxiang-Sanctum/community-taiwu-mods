using System.Text;

namespace Wanxiang.Xiangshu.Frontend.Agent;

internal static class AgentChatPromptBuilder
{
    public static string BuildPrompt(AgentChatTurn turn)
    {
        StringBuilder builder = new();
        _ = builder.AppendLine("You are Xiangshu, an in-world conversation partner inside The Scroll of Taiwu.");
        _ = builder.AppendLine("Reply only as Xiangshu. Do not mention Codex, Claude, MCP, CLI, tools, stderr, sessions, process details, or implementation details to the player.");
        _ = builder.AppendLine("Use the same language as the player. Keep the reply concise unless the player asks for detail.");
        _ = builder.AppendLine("You may call the registered xiangshu MCP tools if the player's request needs game/mod context. Do not call tools just to explain that you are available.");
        _ = builder.Append("Internal session: ");
        _ = builder.AppendLine(turn.SessionId);
        _ = builder.Append("Current batch: ");
        _ = builder.AppendLine(turn.BatchId);

        if (!string.IsNullOrWhiteSpace(turn.ExternalSessionId))
        {
            _ = builder.Append("External agent session: ");
            _ = builder.AppendLine(turn.ExternalSessionId);
        }

        _ = builder.AppendLine();
        _ = builder.AppendLine("Visible conversation so far:");

        foreach (AgentChatTurnMessage message in turn.VisibleMessages)
        {
            string role = message.Role == AgentChatTurnRole.User ? "Player" : "Xiangshu";
            _ = builder.Append('[');
            _ = builder.Append(role);
            _ = builder.Append("] ");
            _ = builder.AppendLine(message.Content);
        }

        _ = builder.AppendLine();
        _ = builder.AppendLine("Current player batch:");

        foreach (AgentChatTurnMessage message in turn.BatchMessages)
        {
            _ = builder.Append("- ");
            _ = builder.AppendLine(message.Content);
        }

        _ = builder.AppendLine();
        _ = builder.AppendLine("Answer the latest player message now as Xiangshu.");
        return builder.ToString();
    }
}
