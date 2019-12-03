using System.Linq;
using System.Collections.Generic;

using MissionControl.Data;

using BattleTech.Data;

public static class MetaDatabaseExtensions {
  public static List<Mood_MDD> GetMoods(this MetadataDatabase mdd) {
    return mdd.Query<Mood_MDD>("SELECT * FROM Mood").ToList<Mood_MDD>();
  }

  public static List<ContractType_MDD> GetCustomContractTypes(this MetadataDatabase mdd) {
    return mdd.Query<ContractType_MDD>("SELECT * FROM ContractType WHERE ContractTypeID >= 1000").ToList<ContractType_MDD>();
  }

  public static EncounterLayer_MDD InsertOrUpdateEncounterLayer(this MetadataDatabase mdd, EncounterLayer encounterLayer) {
    mdd.Execute("INSERT OR REPLACE INTO EncounterLayer (EncounterLayerID, MapID, Name, FriendlyName, Description, BattleValue, ContractTypeID, EncounterLayerGUID, TagSetID, IncludeInBuild) values(@EncounterLayerID, @MapID, @Name, @FriendlyName, @Description, @BattleValue, @ContractTypeID, @EncounterLayerGUID, @TagSetID, @IncludeInBuild)", new {
      EncounterLayerID = encounterLayer.EncounterLayerId,
      MapID = encounterLayer.MapId,
      Name = encounterLayer.Name,
      FriendlyName = encounterLayer.FriendlyName,
      Description = encounterLayer.Description,
      BattleValue = encounterLayer.BattleValue,
      ContractTypeID = encounterLayer.ContractTypeId,
      EncounterLayerGUID = encounterLayer.EncounterLayerGuid,
      TagSetID = encounterLayer.TagSetId,
      IncludeInBuild = encounterLayer.IncludeInBuild
    }, null, null, null);
    return mdd.SelectEncounterLayerByID(encounterLayer.EncounterLayerId);
  }

  public static EncounterLayer_MDD SelectEncounterLayerByID(this MetadataDatabase mdd, string encounterLayerId) {
    return mdd.Query<EncounterLayer_MDD>("SELECT * FROM EncounterLayer WHERE EncounterLayerID=@encounterLayerId", new {
      EncounterLayerID = encounterLayerId
    }, null, true, null, null).FirstOrDefault<EncounterLayer_MDD>();
  }
}