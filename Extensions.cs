using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AllaganKillFeed;

internal static class Extensions
{
    public static unsafe Utf8StringBuilder AppendBattleChara(this Utf8StringBuilder stringBuilder, BattleChara* battleChara)
    {
        stringBuilder.Append(battleChara->Name);
        // ReSharper disable once InvertIf
        if (battleChara->ClassJob > 0)
        {
            stringBuilder.Append(" ");
            stringBuilder.Append(MainPlugin.SeStringEvaluator.Service.EvaluateFromAddon(37, [MainPlugin.DataManager.Service.GetExcelSheet<ClassJob>().GetRow(battleChara->ClassJob).Abbreviation]));
        }
        return stringBuilder;
    }
    
    public static unsafe BattleChara* GetBattleCharaByEntityId(this GameObjectManager.ObjectArrays gameObjectArrays, uint entityId)
    {
        var gameObject = gameObjectArrays.GetObjectByEntityId(entityId);
        if (gameObject == null || gameObject->ObjectKind is not (ObjectKind.Pc or ObjectKind.BattleNpc)) return null;
        return (BattleChara*)gameObject;
    }
}