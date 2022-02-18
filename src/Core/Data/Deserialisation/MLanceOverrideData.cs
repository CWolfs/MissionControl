using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using BattleTech;
using BattleTech.Framework;
using HBS.Collections;

namespace MissionControl.Data {
  public class MLanceOverrideData {
    [JsonProperty("lanceKey")]
    public string LanceKey { get; set; }

    [JsonProperty("lanceDefId")]
    public string LanceDefId { get; set; } = "Tagged";

    [JsonProperty("supportAutofill")]
    public bool SupportAutofill { get; set; } = true;

    [JsonProperty("lanceTagSet")]
    public MTagSetData LanceTagSet { get; set; } = new MTagSetData();

    [JsonProperty("lanceExcludedTagSet")]
    public MTagSetData LanceExcludedTagSet { get; set; } = new MTagSetData();

    [JsonProperty("spawnEffectTags")]
    public MTagSetData SpawnEffectTags { get; set; } = new MTagSetData();

    [JsonProperty("lanceDifficultyAdjustment")]
    public int LanceDifficultyAdjustment { get; set; } = 0;

    [JsonProperty("unitSpawnPointOverrideList")]
    public List<MUnitSpawnPointOverrideData> UnitSpawnPointOverrides { get; set; } = new List<MUnitSpawnPointOverrideData>();

    public List<UnitSpawnPointOverride> GetUnitSpawnPointOverrideList() {
      List<UnitSpawnPointOverride> unitSpawnPointOverrideList = new List<UnitSpawnPointOverride>();

      if (UnitSpawnPointOverrides.Count <= 0) {  // handle pure lance def references in the `lances` folder
        for (int i = 0; i < 4; i++) {
          UnitSpawnPointOverride unitSpawnPointOverride = new UnitSpawnPointOverride();
          unitSpawnPointOverride.unitDefId = UnitSpawnPointOverride.UseLance;
          unitSpawnPointOverride.pilotDefId = UnitSpawnPointOverride.UseLance;
          unitSpawnPointOverrideList.Add(unitSpawnPointOverride);
        }
      } else {
        foreach (MUnitSpawnPointOverrideData unitSpawnPointOverideData in UnitSpawnPointOverrides) {
          UnitSpawnPointOverride unitSpawnPointOverride = new UnitSpawnPointOverride();
          unitSpawnPointOverride.unitType = (UnitType)Enum.Parse(typeof(UnitType), unitSpawnPointOverideData.UnitType);
          unitSpawnPointOverride.unitDefId = unitSpawnPointOverideData.UnitDefId;

          unitSpawnPointOverride.unitTagSet = new TagSet(unitSpawnPointOverideData.UnitTagSet.TagSetSourceFile);
          unitSpawnPointOverride.unitTagSet.AddRange(unitSpawnPointOverideData.UnitTagSet.Items);

          unitSpawnPointOverride.unitExcludedTagSet = new TagSet(unitSpawnPointOverideData.UnitExcludedTagSet.TagSetSourceFile);
          unitSpawnPointOverride.unitExcludedTagSet.AddRange(unitSpawnPointOverideData.UnitExcludedTagSet.Items);

          unitSpawnPointOverride.spawnEffectTags = new TagSet(unitSpawnPointOverideData.SpawnEffectTags.TagSetSourceFile);
          unitSpawnPointOverride.spawnEffectTags.AddRange(unitSpawnPointOverideData.SpawnEffectTags.Items);

          unitSpawnPointOverride.pilotDefId = unitSpawnPointOverideData.PilotDefId;

          unitSpawnPointOverride.pilotTagSet = new TagSet(unitSpawnPointOverideData.PilotTagSet.TagSetSourceFile);
          unitSpawnPointOverride.pilotTagSet.AddRange(unitSpawnPointOverideData.PilotTagSet.Items);

          unitSpawnPointOverride.pilotExcludedTagSet = new TagSet(unitSpawnPointOverideData.PilotExcludedTagSet.TagSetSourceFile);
          unitSpawnPointOverride.pilotExcludedTagSet.AddRange(unitSpawnPointOverideData.PilotExcludedTagSet.Items);

          unitSpawnPointOverrideList.Add(unitSpawnPointOverride);
        }
      }

      return unitSpawnPointOverrideList;
    }
  }
}