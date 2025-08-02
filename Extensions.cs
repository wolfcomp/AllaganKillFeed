using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;
using Lumina.Text;

namespace AllaganKillFeed;

internal static class Extensions
{
    public static unsafe SeStringBuilder AppendBattleChara(this SeStringBuilder seStringBuilder,
        BattleChara* battleChara)
    {
        seStringBuilder.Append(battleChara->Name);
        // ReSharper disable once InvertIf
        if (battleChara->ClassJob > 0)
        {
            seStringBuilder.Append(" ");
            seStringBuilder.Append(MainPlugin.SeStringEvaluator.Service.EvaluateFromAddon(37, [MainPlugin.DataManager.Service.GetExcelSheet<ClassJob>().GetRow(battleChara->ClassJob).Abbreviation]));
        }
        return seStringBuilder;
    }
    
    public static unsafe BattleChara* GetBattleCharaByEntityId(this GameObjectManager.ObjectArrays gameObjectArrays, uint entityId)
    {
        var gameObject = gameObjectArrays.GetObjectByEntityId(entityId);
        if (gameObject == null || gameObject->ObjectKind is not (ObjectKind.Pc or ObjectKind.BattleNpc)) return null;
        return (BattleChara*)gameObject;
    }
}