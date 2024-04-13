using BattleTech;
using BattleTech.Framework;
using BattleTech.Serialization;

using HBS.Util;

using MissionControl.Data.Refs;
using MissionControl.EncounterNodes.Dropship;

using System.Collections.Generic;

namespace MissionControl.Result {
  public class AnimateDropshipResult : DesignResult {
    public enum DropshipAnimateCommand {
      INVALID_UNSET,
      Land,
      Takeoff,
      Flyby
    }

    [SerializableMember(SerializationTarget.All)]
    public CustomDropshipLandingSpotRef dropshipLandingSpotRef = new CustomDropshipLandingSpotRef();

    [SerializableMember(SerializationTarget.All)]
    public DropshipAnimateCommand animateCommand;

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      base.Trigger(inMessage, triggeringName);
      CustomDropshipLandingSpotGameLogic dropshipLandingSpot = dropshipLandingSpotRef.GetEncounterObject(combat.ItemRegistry);
      List<DropshipGameLogic> dropships = dropshipLandingSpot.GetDropships();

      foreach (DropshipGameLogic dropship in dropships) {
        if (!dropshipLandingSpot.IsDropshipAlive(dropship)) {
          dropshipLandingSpot.LogWarning("Tried to animate a dead dropship. Aborting animation.");
        } else if (dropshipLandingSpot != null) {
          switch (animateCommand) {
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
      return base.Size() + dropshipLandingSpotRef.Size() + HBS.Util.Serialization.StorageSpaceEnum(animateCommand);
    }

    public override void Save(SerializationStream stream) {
      base.Save(stream);
      dropshipLandingSpotRef.Save(stream);
      stream.PutEnum(animateCommand);
    }

    public override void Load(SerializationStream stream) {
      dropshipLandingSpotRef.Load(stream);
      animateCommand = stream.GetEnum<DropshipAnimateCommand>();
    }

    public override void ReattachReferences(Dictionary<string, EncounterObjectGameLogic> encounterObjectDictionary) {
      base.ReattachReferences(encounterObjectDictionary);
      dropshipLandingSpotRef.ReattachReference(encounterObjectDictionary);
    }
  }
}
