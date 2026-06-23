#if NET8_0
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Mod;

namespace Wanxiang.Taiwu.ModRpc;

internal static class BackendTransport
{
    internal static void Register(
        string localModId,
        string methodName,
        Action<DataContext, SerializableModData> handler)
    {
        DomainManager.Mod.AddModMethod(
            localModId,
            methodName,
            handler);
    }

    internal static void Register(
        string localModId,
        string methodName,
        Func<DataContext, SerializableModData, SerializableModData> handler)
    {
        DomainManager.Mod.AddModMethod(
            localModId,
            methodName,
            handler);
    }

    internal static void PublishDisplayEvent(string localModId, string customData)
    {
        DomainManager.Mod.AddModDisplayEvent(
            localModId,
            customData);
    }
}
#endif
