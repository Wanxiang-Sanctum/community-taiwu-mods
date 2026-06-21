#if NET8_0
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Mod;

namespace Wanxiang.Taiwu.ModRpc;

internal static class BackendTransport
{
    internal static void Register(
        string modId,
        string methodName,
        Action<DataContext, SerializableModData> handler)
    {
        DomainManager.Mod.AddModMethod(
            modId,
            methodName,
            handler);
    }

    internal static void Register(
        string modId,
        string methodName,
        Func<DataContext, SerializableModData, SerializableModData> handler)
    {
        DomainManager.Mod.AddModMethod(
            modId,
            methodName,
            handler);
    }

    internal static void PublishDisplayEvent(string modId, string customData)
    {
        DomainManager.Mod.AddModDisplayEvent(
            modId,
            customData);
    }
}
#endif
