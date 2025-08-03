using Dalamud.Plugin;
using Dalamud.Plugin.Services;

#pragma warning disable SeStringEvaluator

namespace AllaganKillFeed;

public class MainPlugin : IDalamudPlugin
{
    private static DalamudServiceIntermediate<IFramework> framework = null!;
    private static DalamudServiceIntermediate<IPluginLog> logger = null!;
    internal static DalamudServiceIntermediate<IDataManager> DataManager = null!;
    private readonly IDalamudPluginInterface pluginInterface;
    internal static DalamudServiceIntermediate<ISeStringEvaluator> SeStringEvaluator = null!;
    internal static DalamudServiceIntermediate<IGameInteropProvider> GameInteropProvider = null!;
    private readonly PacketCapture packetCapture;

    public MainPlugin(IDalamudPluginInterface pluginInterface)
    {
        framework = new DalamudServiceIntermediate<IFramework>(pluginInterface);
        // framework.Service.Update += Service_Update;
        pluginInterface.UiBuilder.Draw += Draw;
        this.pluginInterface = pluginInterface;
        logger = new DalamudServiceIntermediate<IPluginLog>(pluginInterface);
        DataManager = new DalamudServiceIntermediate<IDataManager>(pluginInterface);
        SeStringEvaluator = new DalamudServiceIntermediate<ISeStringEvaluator>(pluginInterface);
        GameInteropProvider = new DalamudServiceIntermediate<IGameInteropProvider>(pluginInterface);
        packetCapture = new PacketCapture();
    }

    private void Draw()
    {
        NotificationDrawer.Draw();
    }

    public void Dispose()
    {
        // framework.Service.Update -= Service_Update;
        pluginInterface.UiBuilder.Draw -= Draw;
        framework.Dispose();
        logger.Dispose();
        DataManager.Dispose();
        SeStringEvaluator.Dispose();
        GameInteropProvider.Dispose();
        packetCapture.Dispose();
    }
}