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
    [Description("Sends an intermediate Xiangshu reply to the current in-game chat session.")]
    public Task<string> SendIntermediateReplyAsync(
        [Description("Short text to show to the player as Xiangshu.")]
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
    [Description("Executes trusted C# code inside the Wanxiang.Xiangshu frontend or backend plugin process.")]
    public Task<string> ExecuteCSharpScriptAsync(
        [Description("Target plugin side. Valid values: frontend, backend.")]
        string side,
        [Description("Complete C# source to compile. Define public static class XiangshuScript with Execute or ExecuteAsync taking XiangshuScriptGlobals.")]
        string script,
        [Description("Optional JSON object passed to the script as globals.Arguments. Non-string values are passed as compact JSON strings.")]
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
