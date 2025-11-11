using UnityEngine;

using MissionControl.Data;
using MissionControl.EncounterNodes.Generator;

using System.Collections.Generic;

using HBS.Collections;

namespace MissionControl.EncounterFactories {
  public static class GeneratorFactory {
    private static GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject go = new GameObject(name ?? "Generator");
      go.transform.parent = parent.transform;
      go.transform.localPosition = Vector3.zero;

      return go;
    }

    public static PowerGeneratorGameLogic CreatePowerGenerator(GameObject parent, string name, string guid, List<string> generatorTags, List<string> powerConsumersTags, OnGeneratorDestructionType onGeneratorDestructionTypeRaw) {
      GameObject dropshipLandingSpoGameObject = CreateGameObject(parent, name);

      PowerGeneratorGameLogic powerGeneratorGameLogic = dropshipLandingSpoGameObject.AddComponent<PowerGeneratorGameLogic>();
      powerGeneratorGameLogic.encounterObjectGuid = guid;

      powerGeneratorGameLogic.GeneratorTags = new TagSet(generatorTags);
      powerGeneratorGameLogic.PowerConsumersTags = new TagSet(powerConsumersTags);
      powerGeneratorGameLogic.OnGeneratorDestructionType = onGeneratorDestructionTypeRaw;

      return powerGeneratorGameLogic;
    }
  }
}