using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
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
        "Checks whether the Wanxiang.Guanxiangtai frontend and backend plugins are reachable through the live game runtime. "
        + "Returns JSON with frontend/backend status kind=available or kind=unavailable(reason). "
        + "A successful tool call already confirms MCP server transport and authorization.")]
    public Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return PluginIpcProxy.GetStatusJsonAsync(cancellationToken);
    }

    [McpServerTool(
        Name = "guanxiangtai_launch_taiwu",
        Destructive = false,
        Idempotent = true,
        ReadOnly = false)]
    [Description(
        "Requests Steam to launch The Scroll of Taiwu through steam://rungameid/838350, then waits for both "
        + "Guanxiangtai frontend and backend plugins to become reachable or for the fixed startup wait to expire. "
        + "Returns JSON with launch request facts and runtimeReady frontend/backend readiness.")]
    public Task<string> LaunchTaiwuAsync(CancellationToken cancellationToken = default)
    {
        return TaiwuLifecycle.LaunchAsync(cancellationToken);
    }

    [McpServerTool(
        Name = "guanxiangtai_stop_taiwu",
        Destructive = true,
        Idempotent = true,
        ReadOnly = false)]
    [Description(
        "Stops The Scroll of Taiwu for development workflows, then waits until Taiwu frontend and matched backend "
        + "processes are gone or the fixed stop wait expires. "
        + "Returns JSON with stop attempt facts and process exit readiness.")]
    public Task<string> StopTaiwuAsync(
        [Description("Stop strategy.")]
        McpTaiwuStopMethod method = McpTaiwuStopMethod.Force,
        CancellationToken cancellationToken = default)
    {
        return TaiwuLifecycle.StopAsync(method, cancellationToken);
    }

    [McpServerTool(
        Name = "guanxiangtai_restart_taiwu",
        Destructive = true,
        Idempotent = false,
        ReadOnly = false)]
    [Description(
        "Restarts The Scroll of Taiwu for development workflows. It stops Taiwu with the selected strategy, launches "
        + "through steam://rungameid/838350 only after stop completes, then waits for both Guanxiangtai plugins "
        + "to become reachable or for the fixed startup wait to expire. "
        + "Returns JSON with stop and launch attempt facts.")]
    public Task<string> RestartTaiwuAsync(
        [Description("Stop strategy used before launch.")]
        McpTaiwuStopMethod stopMethod = McpTaiwuStopMethod.Force,
        CancellationToken cancellationToken = default)
    {
        return TaiwuLifecycle.RestartAsync(stopMethod, cancellationToken);
    }

    [McpServerTool(
        Name = "guanxiangtai_run_csharp_script",
        Destructive = true,
        Idempotent = false,
        ReadOnly = false)]
    [Description(
        "Executes a fully trusted C# compilation unit inside the live Wanxiang.Guanxiangtai frontend or backend plugin process. "
        + "Use it only for live game/mod inspection, verification, or action through plugin APIs. "
        + "Returns JSON with kind=notInvoked(reason, details?) or kind=invoked(outcome), where outcome.kind is "
        + "returnValue(value) or exception(message).")]
    public Task<string> RunCSharpScriptAsync(
        [Description(
            "Target plugin process. Use frontend for UI and frontend runtime state; use backend for backend plugin state "
            + "and backend-side game APIs.")]
        McpPluginSide targetSide,
        [Description(
            "Complete C# compilation unit. It must define the public static entry type "
            + "Wanxiang.Guanxiangtai.Scripting.GuanxiangtaiScript with exactly one public static Execute or ExecuteAsync "
            + "method that takes Wanxiang.Guanxiangtai.Scripting.GuanxiangtaiScriptGlobals. "
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
}
