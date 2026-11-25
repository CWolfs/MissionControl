using BattleTech;

using HBS.Collections;
using HBS.Util;

using MissionControl.Data;

using System.Collections.Generic;

namespace MissionControl.EncounterNodes.Generator {
  public class PowerGeneratorGameLogic : EncounterObjectGameLogic {
    public override TaggedObjectType Type => TaggedObjectType.Logic;

    public TagSet GeneratorTags { get; set; } = new TagSet();
    public TagSet PowerConsumersTags { get; set; } = new TagSet();
    public OnGeneratorDestructionType OnGeneratorDestructionType { get; set; } = OnGeneratorDestructionType.DestroyConsumers;

    public BattleTech.Building GeneratorBuilding { get; set; }

    public override void OnEnterActive() {
      base.OnEnterActive();
      FindGeneratorBuilding();
    }

    private void FindGeneratorBuilding() {
      List<ITaggedItem> validGenerators = Combat.ItemRegistry.GetObjectsOfTypeWithTagSet(TaggedObjectType.Building, GeneratorTags);
      if (validGenerators.Count > 0) {
        GeneratorBuilding = validGenerators[0] as BattleTech.Building;
        Main.LogDebug($"[PowerGeneratorGameLogic.FindGeneratorBuilding] Found valid generator '{GeneratorBuilding.DisplayName}' for tags '{GeneratorTags}'.");
      } else {
        Main.Logger.LogWarning($"[PowerGeneratorGameLogic.FindGeneratorBuilding] No valid generator found for tags '{GeneratorTags}' on '{name}'.");
      }
    }


    public override void AlwaysInit(CombatGameState combat) {
      base.AlwaysInit(combat);

      messageMemory.TrackMethod(MessageCenterMessageType.OnActorDestroyed, OnActorDestroyed);
    }

    public override void SubscribeToMessages(bool shouldAdd) {
      base.SubscribeToMessages(shouldAdd);

      messageMemory.Subscribe(MessageCenterMessageType.OnActorDestroyed, OnActorDestroyed, shouldAdd);
    }

    public void OnActorDestroyed(MessageCenterMessage message) {
      Main.LogDebug($"[PowerGeneratorGameLogic.OnActorDestroyed] Received message '{message.MessageType.ToString()}'");

      ActorDestroyedMessage actorDestroyedMessage = message as ActorDestroyedMessage;
      if (actorDestroyedMessage == null) return;

      if (GeneratorBuilding == null) FindGeneratorBuilding();
      if (GeneratorBuilding == null) return;

      if (actorDestroyedMessage.DestroyedGuid == GeneratorBuilding.GUID) {
        Main.LogDebug($"[PowerGeneratorGameLogic.OnActorDestroyed] Generator building '{GeneratorBuilding}' was destroyed.");
        ActOnGeneratorDestruction();
      }
    }

    public void ActOnGeneratorDestruction() {
      switch (OnGeneratorDestructionType) {
        case OnGeneratorDestructionType.DestroyConsumers: DestroyPowerConsumers(); break;
        case OnGeneratorDestructionType.DisableConsumers: DisablePowerConsumers(); break;
        default: Main.LogDebug($"[PowerGeneratorGameLogic.ActOnGeneratorDestruction] No support for OnGeneratorDestructionType '{OnGeneratorDestructionType}'."); break;
      }
    }

    public void DestroyPowerConsumers() {
      List<ITaggedItem> validPowerConsumers = Combat.ItemRegistry.GetObjectsWithTagSet(PowerConsumersTags);
      Main.LogDebug($"[PowerGeneratorGameLogic.DestroyPowerConsumers] Found '{validPowerConsumers.Count}' valid power consumers for tags '{PowerConsumersTags}'.");

      foreach (ITaggedItem powerConsumer in validPowerConsumers) {
        Main.LogDebug($"[PowerGeneratorGameLogic.DestroyPowerConsumers] Destroying power consumer '{powerConsumer.DisplayName}'.");
        EncounterLayerParent.EnqueueLoadAwareMessage(new DestroyActorMessage(GeneratorBuilding.GUID, powerConsumer.GUID));
      }
    }

    public void DisablePowerConsumers() {
      List<ITaggedItem> validPowerConsumers = Combat.ItemRegistry.GetObjectsWithTagSet(PowerConsumersTags);
      Main.LogDebug($"[PowerGeneratorGameLogic.DisablePowerConsumers] Found '{validPowerConsumers.Count}' valid power consumers for tags '{PowerConsumersTags}'.");

      foreach (ITaggedItem powerConsumer in validPowerConsumers) {
        AbstractActor actor = powerConsumer as AbstractActor;

        if (actor == null) {
          Main.Logger.LogWarning($"[PowerGeneratorGameLogic.DisablePowerConsumers] Power consumer '{powerConsumer.DisplayName}' on '{name}' is not a unit so it can't be disabled. Only turrets and vehicles are supported.");
          continue;
        }

        if (actor is Mech) {
          Main.Logger.LogWarning($"[PowerGeneratorGameLogic.DisablePowerConsumers] Power consumer '{actor.DisplayName}' on '{name}' is a mech so it can't be disabled. Only turrets and vehicles are supported.");
          continue;
        }

        if (actor.IsDead || actor.IsFlaggedForDeath || actor.IsShutDown) continue;

        Main.LogDebug($"[PowerGeneratorGameLogic.DisablePowerConsumers] Disabling power consumer '{actor.DisplayName}'.");
        actor.IsShutDown = true;
        Combat.MessageCenter.PublishMessage(new FloatieMessage(GeneratorBuilding.GUID, actor.GUID, "POWER LOST", FloatieMessage.MessageNature.Debuff));
      }
    }

    public override string ToJSON() {
      return JSONSerializationUtility.ToJSON(this);
    }

    public override void FromJSON(string json) {
      JSONSerializationUtility.FromJSON(this, json);
    }

    public override string GenerateJSONTemplate() {
      return JSONSerializationUtility.ToJSON(new PowerGeneratorGameLogic());
    }
  }
}