using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using BattleTech.Data;

public static class MapsAndEncountersMissionControlExtensions {
  public static List<Mood_MDD> GetMoods(this MetadataDatabase mdd) {
    return mdd.Query<Mood_MDD>("SELECT * FROM Mood").ToList<Mood_MDD>();
  }
}