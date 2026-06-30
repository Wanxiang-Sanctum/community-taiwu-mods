using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ModelContextProtocol.Server;

namespace Wanxiang.Guanxiangtai.McpServer;

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
        Name = "guanxiangtai_status",
        Destructive = false,
        Idempotent = true,
        ReadOnly = true)]
    [Description(
        "Reports whether the Wanxiang.Guanxiangtai frontend and backend plugins can answer internal status IPC requests. "
        + "The result describes only those game-side IPC checks; it does not describe MCP server availability or expose internal endpoint addresses.")]
    public Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return PluginIpcProxy.GetStatusJsonAsync(cancellationToken);
    }

    [McpServerTool(
        Name = "guanxiangtai_run_csharp_script",
        Destructive = true,
        Idempotent = false,
        ReadOnly = false)]
    [Description(
        "Executes a fully trusted C# compilation unit inside the live Wanxiang.Guanxiangtai frontend or backend plugin process. "
        + "Use it when current game/mod state affects the answer, or when the user's goal requires plugin APIs for inspection, "
        + "verification, or action. Do not use it for static knowledge or tasks that do not require live game/mod state. "
        + "Before mutation, read current state and narrow the target, effect, and verification path from the user's wording "
        + "and workspace guidance. "
        + "The tool returns JSON describing invocation facts: notInvoked(reason, details?), invoked(returnValue(value)), "
        + "or invoked(exception(message)). Judge whether the script met your intent from that outcome.")]
    public Task<string> RunCSharpScriptAsync(
        [Description(
            "Target plugin side: frontend for UI and frontend runtime state; "
            + "backend for backend plugin state and backend-side game APIs. If the side is uncertain, avoid mutation "
            + "until a minimal read-only script confirms where the needed state lives.")]
        string targetSide,
        [Description(
            "Complete C# compilation unit, not a snippet. Include using directives and define exactly one "
            + "public static non-generic class whose full name is Wanxiang.Guanxiangtai.Scripting.GuanxiangtaiScript "
            + "with public static Execute or ExecuteAsync "
            + "taking Wanxiang.Guanxiangtai.Scripting.GuanxiangtaiScriptGlobals. "
            + "The host supplies the Wanxiang.Guanxiangtai scripting contract reference. "
            + "Frontend hosts also supply explicitly enabled frontend capability references such as UniTask. "
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
}
