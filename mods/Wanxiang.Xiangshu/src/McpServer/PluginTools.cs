using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
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
        + "Use it only for live game/mod inspection, verification, or action through plugin APIs. "
        + "Returns JSON with kind=notInvoked(reason, details?) or kind=invoked(outcome), where outcome.kind is "
        + "returnValue(value) or exception(message).")]
    public Task<string> RunCSharpScriptAsync(
        [Description(
            "Target plugin process. Use frontend for UI, chat window, and frontend runtime state; use backend for "
            + "backend plugin state and backend-side game APIs.")]
        McpPluginSide targetSide,
        [Description(
            "Complete C# compilation unit. It must define the public static entry type "
            + "Wanxiang.Xiangshu.Scripting.XiangshuScript with exactly one public static Execute or ExecuteAsync "
            + "method that takes Wanxiang.Xiangshu.Scripting.XiangshuScriptGlobals. "
            + "Declare required using directives explicitly; the entry return value is serialized as JSON.")]
        string script,
        [Description(
            "Required JSON object passed through globals.Arguments. Use an empty object when no arguments are needed.")]
        Dictionary<string, JsonElement> arguments,
        [Description(
            "Thread used to invoke the script entry. Use mainThread for Unity UI/object access or backend game-domain state access.")]
        McpScriptEntryThread entryThread = McpScriptEntryThread.Current,
        CancellationToken cancellationToken = default)
    {
        return PluginIpcProxy.RunCSharpScriptAsync(
            targetSide,
            script,
            arguments,
            entryThread,
            cancellationToken);
    }

    [McpServerTool(
        Name = "xiangshu_capture_player_view",
        Destructive = false,
        Idempotent = false,
        ReadOnly = true)]
    [Description(
        "Captures the current full-screen, native-resolution player-visible Unity frontend view as PNG image content. "
        + "Use it for visible screen facts, screen-coordinate targeting evidence, UI inspection, or visual verification. "
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
