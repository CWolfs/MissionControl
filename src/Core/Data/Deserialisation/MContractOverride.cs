using System;
using System.Collections.Generic;
using System.Reflection;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using HBS.Collections;
using HBS.Util;

using fastJSON;

using Harmony;

namespace MissionControl.Data {
  [Serializable]
  public class MContractOverride : ContractOverride {
    private Traverse baseCachedJsonField;
    private Traverse baseRehydratedField;

    [JsonSerialized]
    public int minNumberOfPlayerUnits = -1;

    public MContractOverride() {
      baseCachedJsonField = Traverse.Create(this).Field("cachedJson");
      baseRehydratedField = Traverse.Create(this).Field("rehydrated");
    }


    public MContractOverride(ContractOverride contractOverride) : base() {

    }

    private bool PartialRehydratePredicate(string memberName) {
      switch (memberName) {
        default:
          return memberName == "weight";
        case "requirementList":
        case "contractType":
        case "contractTypeID":
        case "difficulty":
          return true;
      }
    }

    public new void FullRehydrate() {
      string cachedJson = (string)baseCachedJsonField.GetValue();
      if (!string.IsNullOrEmpty(cachedJson) && !Rehydrated) {
        JSONSerializationUtility.FromJSON(this, cachedJson);
        UpgradeToDataDrivenEnums();
        baseRehydratedField.SetValue(true);
      }
    }

    public new void FromJSONFull(string json) {
      JSONSerializationUtility.FromJSON(this, json);
      UpgradeToDataDrivenEnums();
      baseRehydratedField.SetValue(true);
    }

    public new void FromJSON(string json) {
      baseCachedJsonField.SetValue(json);
      // Main.LogDebug($"[MContractOverride] json is: '{json}'");
      if (json.Contains("Blackout")) {
        Main.LogDebug($"[MContractOverride] Blackout");
      }
      JSONSerializationUtility.FromJSON(this, json, PartialRehydratePredicate);
      UpgradeToDataDrivenEnums();
      Main.LogDebug($"[MContractOverride] minNumberOfPlayerUnits is: '{minNumberOfPlayerUnits}'");
    }

    public new MContractOverride Copy() {
      Main.LogDebug($"[MContractOverride] Copy");
      ContractOverride contractOverride = base.Copy();
      MContractOverride mContractOverride = new MContractOverride(contractOverride);
      // MContractOverride specific data set
      return mContractOverride;
    }

    public new string GenerateJSONTemplate() {
      return JSONSerializationUtility.ToJSON(new MContractOverride());
    }
  }
}