using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;
using Lumina.Text;

namespace AllaganKillFeed;

internal unsafe class PacketCapture : IDisposable
{
    private Dictionary<uint, uint> lastDamages = new();

    public PacketCapture()
    {
        MainPlugin.GameInteropProvider.Service.InitializeFromAttributes(this);

        processPacketActionEffectHook = MainPlugin.GameInteropProvider.Service.HookFromSignature<ActionEffectHandler.Delegates.Receive>(ActionEffectHandler.Addresses.Receive.String, ProcessPacketActionEffectDetour);
        processPacketActionEffectHook.Enable();
        processPacketActorControlHook.Enable();
    }

    private delegate void ProcessPacketActorControlDelegate(uint entityId, uint type, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, ulong param7, byte isReplay);

    private readonly Hook<ActionEffectHandler.Delegates.Receive> processPacketActionEffectHook;

    [Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", DetourName = nameof(ProcessPacketActorControlDetour))]
    private readonly Hook<ProcessPacketActorControlDelegate> processPacketActorControlHook = null!;

    private void ProcessPacketActionEffectDetour(uint casterEntityId, Character* casterPtr, Vector3* targetPos, ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects, GameObjectId* targetEntityIds)
    {
        processPacketActionEffectHook.Original(casterEntityId, casterPtr, targetPos, header, effects, targetEntityIds);
        var targets = header->NumTargets;
        if (targets == 0) return;
        var gameObjectManager = GameObjectManager.Instance();
        for (var i = 0; i < targets; i++)
        {
            var targetEntityId = targetEntityIds[i].ObjectId;
            var gameObject = gameObjectManager->Objects.GetObjectByEntityId(targetEntityId);
            if (gameObject == null || gameObject->ObjectKind is not (ObjectKind.Pc or ObjectKind.BattleNpc)) continue;
            foreach (var effect in effects[i].Effects)
            {
                if (effect.Type is not (3 or 5 or 6)) continue;
                lastDamages[targetEntityId] = casterEntityId;
            }
        }
    }

    private void ProcessPacketActorControlDetour(uint entityId, uint type, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, ulong param7, byte isReplay)
    {
        processPacketActorControlHook.Original(entityId, type, param1, param2, param3, param4, param5, param6, param7, isReplay);
        if (isReplay != 0) return; // Ignore replays
        var gameObjectManager = GameObjectManager.Instance();
        switch (type)
        {
            case 0x6: // Death
                var battleChara = (BattleChara*)gameObjectManager->Objects.GetObjectByEntityId(entityId);
                if (battleChara == null) return;
                if (battleChara->ObjectKind is not (ObjectKind.Pc or ObjectKind.BattleNpc)) return;
                var seStringBuilder = new SeStringBuilder();
                if (lastDamages.TryGetValue(entityId, out var lastDamageEntityId))
                {
                    var lastDamageGameObject = (BattleChara*)gameObjectManager->Objects.GetObjectByEntityId(lastDamageEntityId);
                    if (lastDamageGameObject != null && lastDamageGameObject->ObjectKind is (ObjectKind.Pc or ObjectKind.BattleNpc))
                    {
                        seStringBuilder.Append(battleChara->NameString);
                        if (battleChara->ClassJob > 0)
                        {
                            seStringBuilder.Append(" ");
                            seStringBuilder.Append(MainPlugin.SeStringEvaluator.Service.EvaluateFromAddon(37, [MainPlugin.DataManager.Service.GetExcelSheet<ClassJob>().GetRow(battleChara->ClassJob).Abbreviation]));
                        }
                        seStringBuilder.Append(" was killed by ");
                        seStringBuilder.Append(lastDamageGameObject->NameString);
                        if (lastDamageGameObject->ClassJob > 0)
                        {
                            seStringBuilder.Append(" ");
                            seStringBuilder.Append(MainPlugin.SeStringEvaluator.Service.EvaluateFromAddon(37, [MainPlugin.DataManager.Service.GetExcelSheet<ClassJob>().GetRow(lastDamageGameObject->ClassJob).Abbreviation]));
                        }
                        seStringBuilder.Append("!");
                    }
                    else
                    {
                        seStringBuilder.Append(battleChara->NameString);
                        if (battleChara->ClassJob > 0)
                        {
                            seStringBuilder.Append(" ");
                            seStringBuilder.Append(MainPlugin.SeStringEvaluator.Service.EvaluateFromAddon(37, [MainPlugin.DataManager.Service.GetExcelSheet<ClassJob>().GetRow(battleChara->ClassJob).Abbreviation]));
                        }
                        seStringBuilder.Append(" was killed!");
                    }
                }
                else
                {
                    seStringBuilder.Append(battleChara->NameString);
                    if (battleChara->ClassJob > 0)
                    {
                        seStringBuilder.Append(" ");
                        seStringBuilder.Append(MainPlugin.SeStringEvaluator.Service.EvaluateFromAddon(37, [MainPlugin.DataManager.Service.GetExcelSheet<ClassJob>().GetRow(battleChara->ClassJob).Abbreviation]));
                    }
                    seStringBuilder.Append(" was killed!");
                }
                var notification = new Notification(TimeSpan.FromSeconds(5), seStringBuilder.ToReadOnlySeString(), "Kill feed");
                NotificationManager.PendingNotifications.Add(notification.ToActiveNotification);
                break;
        }
    }

    public void Dispose()
    {
        processPacketActorControlHook.Dispose();
        processPacketActionEffectHook.Dispose();
    }
}