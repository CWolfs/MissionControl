using BattleTech;

using MissionControl.EncounterNodes.Dropship;

namespace MissionControl.Data.Refs {
  public class CustomDropshipLandingSpotRef : EncounterObjectRef<CustomDropshipLandingSpotGameLogic> {
    public CustomDropshipLandingSpotRef() { }

    public CustomDropshipLandingSpotRef(CustomDropshipLandingSpotGameLogic dls)
      : base(dls) {
    }
  }
}
