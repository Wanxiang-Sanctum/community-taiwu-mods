using System.Diagnostics.CodeAnalysis;
using TaiwuModdingLib.Core.Plugin;
using VContainer;

namespace Xiangshu.Frontend;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "TaiwuRemakePlugin exposes Dispose as the plugin lifecycle hook.")]
[PluginConfig("Xiangshu.Frontend", "WanxiangSanctum", "0.1.0")]
public sealed class FrontendPlugin : TaiwuRemakePlugin
{
    private IDisposable? _containerScope;
    private FrontendIpcServer? _ipcServer;

    public override void Initialize()
    {
        ContainerBuilder builder = new();
        _ = builder.Register<FrontendIpcServer>(Lifetime.Singleton);

        IObjectResolver container = builder.Build();
        _containerScope = container;
        _ipcServer = container.Resolve<FrontendIpcServer>();
        _ipcServer.Start();
    }

    public override void Dispose()
    {
        _ipcServer?.Dispose();
        _ipcServer = null;
        _containerScope?.Dispose();
        _containerScope = null;
    }
}
