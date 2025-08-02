using Dalamud.IoC;
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

    private unsafe void Service_Update(IFramework framework)
    {
        var gameObjectManager = GameObjectManager.Instance();
        foreach (var entityIdCharacters in GameObjectManager.Instance()->Objects.EntityIdSorted)
        {
            var gameObjectPtr = entityIdCharacters.Value;
            if (gameObjectPtr == null) continue;
            if (gameObjectPtr->EntityId != 0xE0000000 && gameObjectManager->Objects.GetObjectByEntityId(gameObjectPtr->OwnerId) != null) continue;
            var objectKind = gameObjectPtr->ObjectKind;
            if (objectKind is not (ObjectKind.Pc or ObjectKind.BattleNpc)) continue;
            var battleCharaPtr = (BattleChara*)gameObjectPtr;
            var entityId = battleCharaPtr->EntityId;
            switch (battleCharaPtr->Health)
            {
                case <= 0 when killedLastSecond.TryAdd(entityId, true):
                {
                    var seStringBuilder = new SeStringBuilder();
                    seStringBuilder.Append(battleCharaPtr->NameString);
                    if (battleCharaPtr->ClassJob > 0)
                    {
                        seStringBuilder.Append(" ");
                        seStringBuilder.Append(SeStringEvaluator.Service.EvaluateFromAddon(37, [DataManager.Service.GetExcelSheet<ClassJob>().GetRow(battleCharaPtr->ClassJob).Abbreviation]));
                    }
                    seStringBuilder.Append(" was killed!");
                    var notification = new Notification(TimeSpan.FromSeconds(5), seStringBuilder.ToReadOnlySeString(), "Kill feed");
                    NotificationManager.PendingNotifications.Add(notification.ToActiveNotification);
                    break;
                }
                case > 0:
                    killedLastSecond.Remove(entityId);
                    break;
            }
        }
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