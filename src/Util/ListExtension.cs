using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using MissionControl;

using BattleTech.Framework;

public static class ListExtensions {
  public static T GetRandom<T>(this List<T> list) {
    if (list.Count >= 0) {
      return list[UnityEngine.Random.Range(0, list.Count)];  
    }

    return default(T);
  }
}