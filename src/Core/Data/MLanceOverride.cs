using System.Collections.Generic;

using BattleTech.Framework;

using HBS.Collections;

namespace MissionControl.Data {
  public class MLanceOverride : LanceOverride {
    public MLanceOverride(string name, TagSet lanceTagSet) {
      this.lanceDefId = "Tagged";

    }

    public MLanceOverride(string name, string lanceDefId, TagSet lanceTagSet, TagSet lanceExcludedTagSet, TagSet spawnEffectTags,
      int lanceDifficultyAdjustment, List<UnitSpawnPointOverride> unitSpawnOverrides) {
      
      this.name = name;
      this.lanceDefId = lanceDefId;
      this.lanceTagSet = lanceTagSet;
      this.lanceExcludedTagSet = lanceExcludedTagSet;
      this.spawnEffectTags = spawnEffectTags;
      this.lanceDifficultyAdjustment = lanceDifficultyAdjustment;
      this.unitSpawnPointOverrideList = unitSpawnOverrides;
    }
  }
}