using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using HBS.Collections;

namespace MissionControl.Data {
  public class MLanceOverride : LanceOverride {

    public string LanceKey { get; set; } = "MC_LANCE_KEY_NOT_SET";
    public bool SupportAutofill { get; set; } = true;

    public MLanceOverride(MLanceOverrideData lanceOverrideData) {
      this.LanceKey = lanceOverrideData.LanceKey;
      this.lanceDefId = lanceOverrideData.LanceDefId;
      this.lanceTagSet = new TagSet(lanceOverrideData.LanceTagSet.TagSetSourceFile);
      this.lanceTagSet.AddRange(lanceOverrideData.LanceTagSet.Items);
      this.lanceExcludedTagSet = new TagSet(lanceOverrideData.LanceExcludedTagSet.TagSetSourceFile);
      this.lanceTagSet.AddRange(lanceOverrideData.LanceExcludedTagSet.Items);
      this.spawnEffectTags = new TagSet(lanceOverrideData.SpawnEffectTags.TagSetSourceFile);
      this.spawnEffectTags.AddRange(lanceOverrideData.SpawnEffectTags.Items);
      this.lanceDifficultyAdjustment = lanceOverrideData.LanceDifficultyAdjustment;
      this.unitSpawnPointOverrideList = lanceOverrideData.GetUnitSpawnPointOverrideList();

      this.SupportAutofill = lanceOverrideData.SupportAutofill;
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

    public MLanceOverride(LanceOverride lanceOverride) {
      this.lanceDefId = lanceOverride.lanceDefId;
      this.lanceTagSet = new TagSet(lanceOverride.lanceTagSet);
      this.lanceExcludedTagSet = new TagSet(lanceOverride.lanceExcludedTagSet);
      this.spawnEffectTags = new TagSet(lanceOverride.spawnEffectTags);
      this.lanceDifficultyAdjustment = lanceOverride.lanceDifficultyAdjustment;
      this.unitSpawnPointOverrideList = lanceOverride.unitSpawnPointOverrideList;
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

    public new MLanceOverride Copy() {
      LanceOverride lanceOveride = base.Copy();
      MLanceOverride mLanceOverride = new MLanceOverride(lanceOveride);
      mLanceOverride.LanceKey = this.LanceKey;
      mLanceOverride.SupportAutofill = this.SupportAutofill;
      return mLanceOverride;
    }
  }
}