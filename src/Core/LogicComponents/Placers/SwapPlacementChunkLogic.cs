using BattleTech.Designed;

namespace MissionControl.LogicComponents.Placers {
  public class SwapPlacementChunkLogic : EmptyCustomChunkGameLogic {
    public string swapFocusGuid { get; set; } = "UNSET";
    public string swapTargetGuid { get; set; } = "UNSET";

    public override void OnStarting() {
      base.OnStarting();
    }
  }
}
