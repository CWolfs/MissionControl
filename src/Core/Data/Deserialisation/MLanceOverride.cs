using System.Collections.Generic;

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