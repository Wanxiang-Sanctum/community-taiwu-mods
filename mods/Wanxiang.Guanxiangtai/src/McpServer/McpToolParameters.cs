using System.ComponentModel;
using System.Text.Json.Serialization;
using Wanxiang.Guanxiangtai.Ipc;

namespace Wanxiang.Guanxiangtai.McpServer;

internal enum McpTaiwuStopMethod
{
    [JsonStringEnumMemberName("force")]
    [Description("Kill matched Taiwu frontend process trees and leftover Backend/GameData.exe processes.")]
    Force = 0,

    [JsonStringEnumMemberName("requestQuit")]
    [Description("Ask the live Guanxiangtai frontend plugin to run the game's quit flow.")]
    RequestQuit = 1,
}

internal enum McpPluginSide
{
    [JsonStringEnumMemberName("frontend")]
    [Description("The frontend plugin process: Unity UI, frontend runtime state, and frontend-only APIs.")]
    Frontend = 0,

    [JsonStringEnumMemberName("backend")]
    [Description("The backend plugin process: backend plugin state and backend-side game APIs.")]
    Backend = 1,
}

internal enum McpScriptEntryThread
{
    [JsonStringEnumMemberName("current")]
    [Description("Invoke the script entry on the IPC handler thread.")]
    Current = 0,

    [JsonStringEnumMemberName("mainThread")]
    [Description("Invoke the script entry on the target side's game main thread.")]
    MainThread = 1,
}

internal static class McpToolParameterConversions
{
    public static string ToIpcRole(this McpPluginSide side)
    {
        return side switch
        {
            McpPluginSide.Frontend => IpcRuntime.FrontendEndpointRole,
            McpPluginSide.Backend => IpcRuntime.BackendEndpointRole,
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Unsupported plugin side."),
        };
    }

    public static IpcScriptEntryThread ToIpcEntryThread(this McpScriptEntryThread entryThread)
    {
        return entryThread switch
        {
            McpScriptEntryThread.Current => IpcScriptEntryThread.Current,
            McpScriptEntryThread.MainThread => IpcScriptEntryThread.MainThread,
            _ => throw new ArgumentOutOfRangeException(
                nameof(entryThread),
                entryThread,
                "Unsupported script entry thread."),
        };
    }
}
