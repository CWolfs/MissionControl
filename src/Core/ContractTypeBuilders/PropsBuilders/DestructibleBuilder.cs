using UnityEngine;

using MissionControl.Data;
using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class DestructibleBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject destructibleGroup;

    private string destructibleGroupName;
    private JObject position;
    private JObject rotation;
    private JArray destructibles;

    public DestructibleBuilder(ContractTypeBuilder contractTypeBuilder, JObject destructibleGroup) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.destructibleGroup = destructibleGroup;

      destructibleGroupName = destructibleGroup["Name"].ToString();
      position = destructibleGroup.ContainsKey("Position") ? (JObject)destructibleGroup["Position"] : null;
      rotation = destructibleGroup.ContainsKey("Rotation") ? (JObject)destructibleGroup["Rotation"] : null;
      destructibles = destructibleGroup.ContainsKey("Destructibles") ? (JArray)destructibleGroup["Destructibles"] : null;
    }

    public override void Build() {
      // Build the group parent
      DestructibleFactory destructibleGroupFactory = new DestructibleFactory();
      GameObject destructibleGroupGO = destructibleGroupFactory.CreateDestructibleFlimsyGroup(destructibleGroupName);

      // Build all the destructibles
      foreach (JObject destructible in destructibles.Children<JObject>()) {
        string destructibleName = destructible["Name"].ToString();
        JObject position = destructible.ContainsKey("Position") ? (JObject)destructible["Position"] : null;
        JObject rotation = destructible.ContainsKey("Rotation") ? (JObject)destructible["Rotation"] : null;

        if (!DataManager.Instance.DestructibleDefs.ContainsKey(destructibleName)) {
          Main.Logger.LogError($"[DestructibleBuilder.Build] No destructible exists with key '{destructibleName}'. Check a PropDestructionDef exists with that key in the 'props/destructible' folder");
        }

        PropDestructibleFlimsyDef propDestructibleDef = DataManager.Instance.DestructibleDefs[destructibleName];
        DestructibleFactory destructibleFactory = new DestructibleFactory(propDestructibleDef);

        GameObject destructibleGO = destructibleFactory.CreateDestructible(destructibleGroupGO, propDestructibleDef.Key);

        if (position != null) {
          SetPosition(destructibleGO, position, exactPosition: true);
        }

        if (rotation != null) {
          SetRotation(destructibleGO, rotation);
        }
      }

      if (this.position != null) {
        SetPosition(destructibleGroupGO, this.position, exactPosition: true);
      }

      if (this.rotation != null) {
        SetRotation(destructibleGroupGO, this.rotation);
      }
    }
  }
}