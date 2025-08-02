using Dalamud.IoC;
using Dalamud.Plugin;

namespace AllaganKillFeed;

public class DalamudServiceIntermediate<T> : IDisposable
    where T : class
{
    [PluginService] public T Service { get; private set; } = null!;
    
    public DalamudServiceIntermediate(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Inject(this);
    }

    public void Dispose()
    {
        Service = null!;
    }

    public static implicit operator T(DalamudServiceIntermediate<T> service) => service.Service;
}