# Contract Type Rulesets

Contract Type Rulesets are what control the manipulation of a mission/contract. To manipulate encounters you create lots of `Logic` objects and submit them to the `EncounterLogic` system. Mission Control then intelligently runs these logic blocks at the correct point as the game runs.

## Example Ruleset
Below is an example of the `AssassinateEncounterRules` used in Mission Control. We'll run through each bit of code to explain what is happening.
```csharp
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Trigger;
using MissionControl.Logic;

namespace MissionControl.Rules {
  public class AssassinateEncounterRules : EncounterRules {
    public AssassinateEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[AssassinateEncounterRules] Setting up rule object references");
      BuildAi();
      BuildSpawns();
      BuildAdditionalLances("AssassinateSpawn", SpawnLogic.LookDirection.AWAY_FROM_TARGET,
        "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    public void BuildAi() {
      EncounterLogic.Add(new IssueFollowLanceOrderTrigger(new List<string>(){ "Employer" }, IssueAIOrderTo.ToLance, new List<string>() { "Player 1" }));
    }

    public void BuildSpawns() {
      Main.Logger.Log("[AssassinateEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "AssassinateSpawn"));
      EncounterLogic.Add(new SpawnLanceAnywhere(this, "AssassinateSpawn", "SpawnerPlayerLance", 400));
      EncounterLogic.Add(new LookAtTarget(this, "SpawnerPlayerLance", "AssassinateSpawn"));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("AssassinateSpawn", EncounterLayerData.gameObject.FindRecursive("Lance_Enemy_AssassinationTarget"));
    }
  }
}
```

## Example Walkthrough

### Set up the right special superclass

```csharp
public class AssassinateEncounterRules : EncounterRules {
```

To create a ruleset you must create a class and extend the `EncounterRules` superclass. This provides lots of inbuilt functionality useful for encounters and allows Mission Control to use it.

### Create a build step

```csharp
public override void Build() {
  Main.Logger.Log("[AssassinateEncounterRules] Setting up rule object references");
  BuildAi();
  BuildSpawns();
  BuildAdditionalLances("AssassinateSpawn", SpawnLogic.LookDirection.AWAY_FROM_TARGET,
    "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
}
```
This is a special method which is required. It is called by Mission Control at the appropriate time to create the initial configuration. From here you can chain into your own methods for organisational purposes. I've used it to chain on Ai, Spawns and the special hook for `Additional Lances` support for this contract type.

The `EncounterRules` superclass gives you access to an easy way of enabling `Additional Lances` for your encounter. Here is the method hook explained in more detail.

```csharp
BuildAdditionalLances(TargetKeyToSpawnEnemyLancesAround, DirectionToLookAtInRelationToEnemyTarget, TargetKeyToSpawnAllyLancesAround, 
  DirectionToLookAtInRelationToAllyTarget, MinDistanceForAlliesToSpawnFromAllyTarget, MaxistanceForAlliesToSpawnFromAllyTarget);
```

The target keys are bound at the bottom of the class with the `LinkObjectReferences` method. This will be explained in a moment.

### Specify AI

You can specify additional AI to be applied to the Core AI behaviour tree. You can do this as follows:

```csharp
public void BuildAi() {
  EncounterLogic.Add(new IssueFollowLanceOrderTrigger(new List<string>(){ "Employer" }, IssueAIOrderTo.ToLance, new List<string>() { "Player 1" }));
}
```

This method is for organisational purposes only. You can set up your AI logic in a method similar to this.

Two things are happening here. Firstly, you add a logic block relating to AI to the `EncounterLogic` property that is provided by the superclass `EncounterRules` your class has access to. The `EncounterLogic` list contains logic blocks of different type and that execute at different points in the game depending on their type. The main types are:

* RESOURCE_REQUEST
* CONTRACT_OVERRIDE_MANIPULATION
* ENCOUNTER_MANIPULATION
* SCENE_MANIPULATION

and these types are executed in that order depeneding on when the game requires them to be executed.

Secondly, the logic block you submit is the AI logic block for assigning all the found targets with tag if `Employer` of type `Lance` to follow the lance with tag `Player 1`. This is used to allow allies who spawn on the encounter boundary to follow the player lance into combat and not get stuck.

### Build Spawns

```csharp
public void BuildSpawns() {
  Main.Logger.Log("[AssassinateEncounterRules] Building spawns rules");
  EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "AssassinateSpawn"));
  EncounterLogic.Add(new SpawnLanceAnywhere(this, "AssassinateSpawn", "SpawnerPlayerLance", 400));
  EncounterLogic.Add(new LookAtTarget(this, "SpawnerPlayerLance", "AssassinateSpawn"));
}
```

This method is for organisational purposes only. You can set up your spawn logic in a method similar to this.

Mission Control provides four spawn logic blocks to be used to move targets around the map. They are:

* SpawnLanceAnywhere
* SpawnLanceAroundTarget
* SpawnLanceMembersAroundTarget
* SpawnLanceAtEdgeBoundary

and there are two orientation logic blocks of:

* LookAtTarget
* LookAwayFromTarget

For each of these you provide different arguments but they all are very similar. You provide the target you wish to move around, the target you wish to be the orientation target (e.g to look at after being moved and to pathfind check to) and sometimes provide the look type (TOWARDS ORIENTATION TARGET, AWAY FROM ORIENTATION TARGET), minimum and maximum spawn distance constraints.

### Link Object References

```csharp
public override void LinkObjectReferences(string mapName) {
  ObjectLookup.Add("AssassinateSpawn", EncounterLayerData.gameObject.FindRecursive("Lance_Enemy_AssassinationTarget"));
}
```

This is a very important method. Almost every logic block requires the use of keys to identify which item to move around the game map. This is how those keys are linked up to the game objects in the game scene / map. In this example, we assign the object found under the `EncounterLayerData` object in the Unity game scene which has the name `Lance_Enemy_AssassinationTarget`, which is a spawner object for the encounter, to the key `AssassinateSpawn`.

To discover the name and unity game scene location of the object to link I highly recommend you use the mod [BTDebug](https://github.com/CWolfs/BTDebug) with the latest releases found in the [Releases](https://github.com/CWolfs/BTDebug/releases) area. It has a runtime inspector that allows you to view what objects are in the game scene / map so you can correctly link up what you need.

## Send Your Custom Ruleset To Mission Control

You're able to provide your custom ruleset to Mission Control and it will use it. Mission Control randomly selects from all the available rulesets for a particular contract type.

To do this you must first create your own rulset by linking your mod to `MissionControl.dll` then creating it as explained above. Then, in your mod code execute code similar to the below example

```csharp
MissionControl.Instance.AddEncounter("Assassinate", myCustomAssassinateRuleset);  // Add a custom assassinate ruleset
```

If for whatever reason you wish to only have your custom rulesets running you can clear all loaded rulesets with the following code:

```csharp
MissionControl.Instance.ClearEncounters();  // Clears all encounters for all contract types
```

and

```csharp
MissionControl.Instance.ClearEncounters("Assassinate");  // Clears all encounters for only the 'Assassinate' contract type
```

## Summary

It's worth reviewing the code of the mod to see what already happens for the default encounter rulesets, especially looking at the `EncounterRules.cs` class to see what the mod already provides for you to use (e.g. it contains properties that link to the encounter object in the Unity game scene you can use).