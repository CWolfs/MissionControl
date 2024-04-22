using BattleTech;
using BattleTech.Framework;
using BattleTech.Serialization;

using HBS.Util;

using MissionControl.Data.Refs;
using MissionControl.EncounterNodes.Dropship;

using System.Collections.Generic;

namespace MissionControl.Result {
  public class CustomAnimateDropshipResult : DesignResult {
    public enum DropshipAnimateCommand {
      Land,
      Takeoff,
      Flyby
    }

    [SerializableMember(SerializationTarget.All)]
    public CustomDropshipLandingSpotRef DropshipLandingSpotRef { get; set; } = new CustomDropshipLandingSpotRef();

    [SerializableMember(SerializationTarget.All)]
    public DropshipAnimateCommand AnimateCommand { get; set; } = DropshipAnimateCommand.Takeoff;

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      base.Trigger(inMessage, triggeringName);
      CustomDropshipLandingSpotGameLogic dropshipLandingSpot = DropshipLandingSpotRef.GetEncounterObject(combat.ItemRegistry);
      List<DropshipGameLogic> dropships = dropshipLandingSpot.DropshipGameLogicList;

      foreach (DropshipGameLogic dropship in dropships) {
        if (!dropshipLandingSpot.IsDropshipAlive(dropship)) {
          dropshipLandingSpot.LogWarning("Tried to animate a dead dropship. Aborting animation.");
        } else if (dropshipLandingSpot != null) {
          switch (AnimateCommand) {
            case DropshipAnimateCommand.Land:
              dropshipLandingSpot.LandDropship(dropship);
              break;
            case DropshipAnimateCommand.Takeoff:
              dropshipLandingSpot.TakeoffDropship(dropship);
              break;
            case DropshipAnimateCommand.Flyby:
              dropshipLandingSpot.FlybyDropship(dropship);
              break;
          }
        }
      }
    }

    public override int Size() {
      return base.Size() + DropshipLandingSpotRef.Size() + HBS.Util.Serialization.StorageSpaceEnum(AnimateCommand);
    }

    public override void Save(SerializationStream stream) {
      base.Save(stream);
      DropshipLandingSpotRef.Save(stream);
      stream.PutEnum(AnimateCommand);
    }

    public override void Load(SerializationStream stream) {
      DropshipLandingSpotRef.Load(stream);
      AnimateCommand = stream.GetEnum<DropshipAnimateCommand>();
    }

    public override void ReattachReferences(Dictionary<string, EncounterObjectGameLogic> encounterObjectDictionary) {
      base.ReattachReferences(encounterObjectDictionary);
      DropshipLandingSpotRef.ReattachReference(encounterObjectDictionary);
    }
  }
}
