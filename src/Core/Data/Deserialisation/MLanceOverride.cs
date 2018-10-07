using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using HBS.Collections;

namespace MissionControl.Data {
  public class MLanceOverride : LanceOverride {
    public MLanceOverride(MLanceOverrideData lanceOverrideData) {
      this.lanceDefId = lanceOverrideData.LanceDefId;
      this.lanceTagSet = new TagSet(lanceOverrideData.LanceTagSet.TagSetSourceFile);
      this.lanceTagSet.AddRange(lanceOverrideData.LanceTagSet.Items);
      this.lanceExcludedTagSet = new TagSet(lanceOverrideData.LanceExcludedTagSet.TagSetSourceFile);
      this.lanceTagSet.AddRange(lanceOverrideData.LanceExcludedTagSet.Items);
      this.spawnEffectTags = new TagSet(lanceOverrideData.SpawnEffectTags.TagSetSourceFile);
      this.spawnEffectTags.AddRange(lanceOverrideData.SpawnEffectTags.Items);
      this.lanceDifficultyAdjustment = lanceOverrideData.LanceDifficultyAdjustment;
      this.unitSpawnPointOverrideList = lanceOverrideData.GetUnitSpawnPointOverrideList();
    }

    public MLanceOverride(LanceDef lanceDef) {
      this.lanceDefId = lanceDef.Description.Id;
      this.lanceTagSet = lanceDef.LanceTags;

      List<UnitSpawnPointOverride> unitSpawnPointOverrides = new List<UnitSpawnPointOverride>();
      foreach (LanceDef.Unit unit in lanceDef.LanceUnits) {
        UnitSpawnPointOverride unitSpawnOverride = new UnitSpawnPointOverride();
        unitSpawnOverride.unitType = unit.unitType;
        unitSpawnOverride.unitDefId = unit.unitId;
        unitSpawnOverride.pilotDefId = unit.pilotId;
        unitSpawnOverride.unitTagSet = unit.unitTagSet;
        unitSpawnOverride.unitExcludedTagSet = unit.excludedUnitTagSet;
        unitSpawnOverride.pilotTagSet = unit.pilotTagSet;
        unitSpawnOverride.pilotExcludedTagSet = unit.excludedPilotTagSet;
        unitSpawnPointOverrides.Add(unitSpawnOverride);
      }

      this.unitSpawnPointOverrideList = unitSpawnPointOverrides;
    }

    public MLanceOverride(string lanceDefId, TagSet lanceTagSet, TagSet lanceExcludedTagSet, TagSet spawnEffectTags,
      int lanceDifficultyAdjustment, List<UnitSpawnPointOverride> unitSpawnOverrides) {

      this.lanceDefId = lanceDefId;
      this.lanceTagSet = lanceTagSet;
      this.lanceExcludedTagSet = lanceExcludedTagSet;
      this.spawnEffectTags = spawnEffectTags;
      this.lanceDifficultyAdjustment = lanceDifficultyAdjustment;
      this.unitSpawnPointOverrideList = unitSpawnOverrides;
    }
  }
}