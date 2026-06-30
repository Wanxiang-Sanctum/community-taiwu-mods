using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using Wanxiang.Xiangshu.Ipc;

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
        "Publishes a brief player-visible Xiangshu message in the current in-game chat while the turn is still running. "
        + "The call succeeds only while the frontend has an active chat dispatch accepting intermediate replies. "
        + "Use it for multi-step or long-running work, or for an early acknowledgement before the final reply. "
        + "Content must already be player-facing Xiangshu text and must not expose implementation details.")]
    public Task<string> SendIntermediateReplyAsync(
        [Description("Brief player-visible Xiangshu text.")]
        string content,
        CancellationToken cancellationToken = default)
    {
        return PluginIpcProxy.SendIntermediateReplyAsync(
            content,
            cancellationToken);
    }

    [McpServerTool(
        Name = "xiangshu_run_csharp_script",
        Destructive = true,
        Idempotent = false,
        ReadOnly = false)]
    [Description(
        "Executes a fully trusted C# compilation unit inside the live Wanxiang.Xiangshu frontend or backend plugin process. "
        + "Use it when current game/mod state affects the answer, or when the player's goal requires plugin APIs for inspection, "
        + "verification, or action. Select it according to the player's goal and workspace guidance. "
        + "Do not use it for ordinary conversation or static knowledge. "
        + "Before mutation, read current state and narrow the target, effect, and verification path from the player's wording "
        + "and workspace guidance. "
        + "The tool returns JSON describing invocation facts: notInvoked(reason, details?), invoked(returnValue(value)), "
        + "or invoked(exception(message)). Judge whether the script met your intent from that outcome.")]
    public Task<string> RunCSharpScriptAsync(
        [Description(
            "Target plugin side: frontend for UI, chat window, and frontend runtime state; "
            + "backend for backend plugin state and backend-side game APIs. If the side is uncertain, avoid mutation "
            + "until a minimal read-only script confirms where the needed state lives.")]
        string targetSide,
        [Description(
            "Complete C# compilation unit, not a snippet. Include using directives and define exactly one "
            + "public static non-generic class whose full name is Wanxiang.Xiangshu.Scripting.XiangshuScript "
            + "with public static Execute or ExecuteAsync "
            + "taking Wanxiang.Xiangshu.Scripting.XiangshuScriptGlobals. "
            + "The host supplies the Wanxiang.Xiangshu scripting contract reference. "
            + "Frontend script hosts include UniTask as a compilation reference. "
            + "Scripts still declare required using directives themselves. "
            + "The entry return value is serialized to JSON.")]
        string script,
        [Description(
            "Thread used to invoke the script entry: current keeps the IPC handler thread; "
            + "mainThread invokes the entry on the target side's game main thread. Use mainThread for Unity UI/object access "
            + "or backend game-domain state access.")]
        string entryThread = "current",
        [Description(
            "Optional JSON object passed through globals.Arguments. String values stay strings; other values become compact JSON strings.")]
        string argumentsJson = "{}",
        CancellationToken cancellationToken = default)
    {
        return PluginIpcProxy.RunCSharpScriptAsync(
            targetSide,
            script,
            entryThread,
            argumentsJson,
            cancellationToken);
    }

    [McpServerTool(
        Name = "xiangshu_capture_player_view",
        Destructive = false,
        Idempotent = false,
        ReadOnly = true)]
    [Description(
        "Captures the current full-screen, native-resolution player-visible Unity frontend view as PNG image content. "
        + "Use it whenever the player's goal depends on visible screen facts, screen-coordinate targeting, UI inspection, "
        + "or visual verification. "
        + "The capture excludes the Xiangshu chat window without changing its visible state or input focus.")]
    public async Task<CallToolResult> CapturePlayerViewAsync(CancellationToken cancellationToken = default)
    {
        IpcCapturePlayerViewResponse response = await PluginIpcProxy.CapturePlayerViewAsync(cancellationToken);

        return new CallToolResult
        {
            Content =
            [
                ImageContentBlock.FromBytes(response.PngBytes, "image/png"),
            ],
        };
    }
}
