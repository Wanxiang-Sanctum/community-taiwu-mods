using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ModelContextProtocol.Server;

namespace Wanxiang.Xiangshu.McpServer;

[McpServerToolType]
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "ModelContextProtocol constructs tool instances through its explicit generic tool registration.")]
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Instance methods keep ModelContextProtocol generic tool registration trim-friendly.")]
internal sealed class PluginTools
{
    [McpServerTool(
        Name = "xiangshu_send_intermediate_reply",
        Destructive = false,
        Idempotent = false,
        ReadOnly = false)]
    [Description(
        "Shows a short provisional Xiangshu message in the current in-game chat before the final reply. "
        + "Use it when a turn needs multiple steps, long-running work, or an early player-visible acknowledgement. "
        + "The content must be brief in-world Xiangshu text, with no MCP, IPC, CLI, log, or implementation details.")]
    public Task<string> SendIntermediateReplyAsync(
        [Description("Brief player-visible text in Xiangshu's voice.")]
        string content,
        CancellationToken cancellationToken = default)
    {
        return PluginIpcProxy.SendIntermediateReplyAsync(
            content,
            cancellationToken);
    }

    [McpServerTool(
        Name = "xiangshu_execute_csharp_script",
        Destructive = true,
        Idempotent = false,
        ReadOnly = false)]
    [Description(
        "Runs fully trusted C# inside the live Wanxiang.Xiangshu frontend or backend plugin process. "
        + "Use it when the answer or requested action needs current game/mod state that only plugin APIs can access. "
        + "Do not use it for ordinary conversation or static knowledge. Mutate game/mod state only when the player intent is clear. "
        + "The tool returns JSON with returnValueJson, error, and diagnostics; check error before relying on returnValueJson.")]
    public Task<string> ExecuteCSharpScriptAsync(
        [Description(
            "Target plugin side: frontend for UI, chat window, and frontend runtime state; "
            + "backend for backend plugin state and backend-side game APIs. If the side is uncertain, avoid mutation "
            + "until a minimal read-only script confirms where the needed state lives.")]
        string side,
        [Description(
            "Complete C# compilation unit, not a snippet. Include using directives and define exactly one public static "
            + "XiangshuScript class with Execute or ExecuteAsync taking XiangshuScriptGlobals; the return value is serialized to JSON.")]
        string script,
        [Description(
            "Optional JSON object passed through globals.Arguments. String values stay strings; other values become compact JSON strings.")]
        string argumentsJson = "{}",
        CancellationToken cancellationToken = default)
    {
        return PluginIpcProxy.ExecuteCSharpScriptAsync(
            side,
            script,
            argumentsJson,
            cancellationToken);
    }
}
