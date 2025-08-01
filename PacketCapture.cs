using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;
using Lumina.Text;

namespace AllaganKillFeed;

internal class PacketCapture : IDisposable
{
    public PacketCapture()
    {
        MainPlugin.GameInteropProvider.Service.InitializeFromAttributes(this);

        processPacketActorControlHook.Enable();
    }

    private delegate void ProcessPacketActorControlDelegate(uint entityId, uint type, uint statusId, uint amount, uint a5, uint source, uint a7, uint a8, uint a9, byte flag);

    [Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", DetourName = nameof(ProcessPacketActorControlDetour))]
    private readonly Hook<ProcessPacketActorControlDelegate> processPacketActorControlHook = null!;

    private unsafe void ProcessPacketActorControlDetour(uint entityId, uint type, uint statusId, uint amount, uint a5, uint source, uint a7, uint a8, uint a9, byte flag)
    {
        processPacketActorControlHook.Original(entityId, type, statusId, amount, a5, source, a7, a8, a9, flag);
        var gameObjectManager = GameObjectManager.Instance();
        switch (type)
        {
            case 0x6: // Death
                var gameObject = gameObjectManager->Objects.GetObjectByEntityId(entityId);
                if (gameObject == null) return;
                if (gameObject->ObjectKind is not (ObjectKind.Pc or ObjectKind.BattleNpc)) return;
                var battleChara = (BattleChara*)gameObject;
                var seStringBuilder = new SeStringBuilder();
                seStringBuilder.Append("Killed: ");
                seStringBuilder.Append(battleChara->NameString);
                if (battleChara->ClassJob > 0)
                {
                    seStringBuilder.Append(" ");
                    seStringBuilder.Append(MainPlugin.SeStringEvaluator.Service.EvaluateFromAddon(37, [MainPlugin.DataManager.Service.GetExcelSheet<ClassJob>().GetRow(battleChara->ClassJob).Abbreviation]));
                }
                seStringBuilder.Append(" was killed!");
                var notification = new Notification(TimeSpan.FromSeconds(5), seStringBuilder.ToReadOnlySeString(), "Kill feed");
                NotificationManager.PendingNotifications.Add(notification.ToActiveNotification);
                break;
        }
    }

    public void Dispose()
    {
        processPacketActorControlHook.Dispose();
    }
}