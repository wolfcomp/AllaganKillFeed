using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;
using Lumina.Text;

#pragma warning disable SeStringEvaluator

namespace AllaganKillFeed;

public class MainPlugin : IDalamudPlugin
{
    private static DalamudServiceIntermediate<IFramework> framework = null!;
    private readonly Dictionary<uint, bool> killedLastSecond = new();
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
        pluginInterface.UiBuilder.Draw += NotificationDrawer.Draw;
        this.pluginInterface = pluginInterface;
        logger = new DalamudServiceIntermediate<IPluginLog>(pluginInterface);
        DataManager = new DalamudServiceIntermediate<IDataManager>(pluginInterface);
        SeStringEvaluator = new DalamudServiceIntermediate<ISeStringEvaluator>(pluginInterface);
        GameInteropProvider = new DalamudServiceIntermediate<IGameInteropProvider>(pluginInterface);
        packetCapture = new PacketCapture();
    }

    public void Dispose()
    {
        // framework.Service.Update -= Service_Update;
        pluginInterface.UiBuilder.Draw -= NotificationDrawer.Draw;
        framework.Dispose();
        logger.Dispose();
        DataManager.Dispose();
        SeStringEvaluator.Dispose();
        GameInteropProvider.Dispose();
    }
}