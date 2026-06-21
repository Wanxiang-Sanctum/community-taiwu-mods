using TaiwuModdingLib.Core.Plugin;
using Wanxiang.Taiwu.ItemGrafts.Contracts;
using Wanxiang.Taiwu.ItemGrafts.Contracts.Internal;
using Wanxiang.Taiwu.ModRpc;

namespace Wanxiang.Taiwu.ItemGrafts.Backend;

/// <summary>
/// 提供观察嫁接宿主物品事实并转发给前端的后端入口。
/// </summary>
public static class BackendInventoryGrafts
{
    private static readonly object SyncRoot = new();

    private static EventHandler<GraftHostEventArgs>? s_eventHandler;

    private static List<IDisposable>? s_methodRegistrations;

    /// <summary>
    /// 为当前太吾 mod 安装后端宿主观察和 RPC 处理器。
    /// </summary>
    /// <param name="plugin">后端插件实例。</param>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> 为 null。</exception>
    public static void Install(TaiwuRemakePlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        string validatedModId = ValidateModId(plugin.ModIdStr);

        lock (SyncRoot)
        {
            bool observerWasInstalled = GraftHostObserver.IsInstalled;

            try
            {
                RpcPeer.Bind(validatedModId);
                GraftHostObserver.Install(validatedModId);

                s_methodRegistrations ??= RegisterMethods();

                if (s_eventHandler is null)
                {
                    EventHandler<GraftHostEventArgs> handler = HandleHostEvent;
                    GraftHostObserver.HostEvent += handler;
                    s_eventHandler = handler;

                    static void HandleHostEvent(object? sender, GraftHostEventArgs hostEvent)
                    {
                        _ = sender;
                        PublishHostEvent(hostEvent);
                    }
                }
            }
            catch
            {
                if (!observerWasInstalled)
                {
                    _ = Uninstall();
                }

                throw;
            }
        }
    }

    /// <summary>
    /// 卸载后端宿主观察并释放已注册的处理器。
    /// </summary>
    /// <returns>移除了已安装的后端观察状态时返回 true；否则返回 false。</returns>
    public static bool Uninstall()
    {
        lock (SyncRoot)
        {
            bool wasInstalled = s_eventHandler is not null
                || s_methodRegistrations is not null
                || GraftHostObserver.IsInstalled;

            if (!wasInstalled)
            {
                return false;
            }

            if (s_eventHandler is not null)
            {
                GraftHostObserver.HostEvent -= s_eventHandler;
                s_eventHandler = null;
            }

            if (s_methodRegistrations is not null)
            {
                DisposeRegistrations(s_methodRegistrations);
            }

            s_methodRegistrations = null;
            GraftHostObserver.Uninstall();
            return true;
        }
    }

    private static List<IDisposable> RegisterMethods()
    {
        List<IDisposable> registrations = [];

        try
        {
            registrations.Add(RpcPeer.Register(
                GraftHostRpcProtocol.SubscribeHostMethodName,
                SubscribeHost));
            registrations.Add(RpcPeer.Register(
                GraftHostRpcProtocol.UnsubscribeHostMethodName,
                UnsubscribeHost));
        }
        catch
        {
            DisposeRegistrations(registrations);
            throw;
        }

        return registrations;
    }

    private static void DisposeRegistrations(List<IDisposable> registrations)
    {
        for (int i = 0; i < registrations.Count; i++)
        {
            registrations[i].Dispose();
        }
    }

    private static string SubscribeHost(
        GameData.Common.DataContext context,
        string payloadJson)
    {
        _ = context;
        ObservedGraftHosts.AddSession(GraftHostRpcProtocol.ReadHostKey(payloadJson));
        return GraftHostRpcProtocol.NullPayload;
    }

    private static string UnsubscribeHost(
        GameData.Common.DataContext context,
        string payloadJson)
    {
        _ = context;
        _ = ObservedGraftHosts.RemoveSession(GraftHostRpcProtocol.ReadHostKey(payloadJson));
        return GraftHostRpcProtocol.NullPayload;
    }

    private static void PublishHostEvent(GraftHostEventArgs hostEvent)
    {
        RpcPeer.Notify(
            GraftHostRpcProtocol.HostEventMethodName,
            GraftHostRpcProtocol.SerializeHostEvent(hostEvent));
    }

    private static string ValidateModId(string modId)
    {
        if (string.IsNullOrWhiteSpace(modId))
        {
            throw new ArgumentException("Mod id must not be empty.", nameof(modId));
        }

        return modId.Trim();
    }
}
