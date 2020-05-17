
using BattleTech;
using BattleTech.Framework;

public static class ObjectiveGameLogicExtensions {
  public static bool IsAnInactiveContractControlledObjective(this ObjectiveGameLogic objectiveGameLogic) {
    EncounterChunkGameLogic chunkGameLogic = objectiveGameLogic.GetComponentInParent<EncounterChunkGameLogic>();
    if ((chunkGameLogic.StartingStatus == EncounterObjectStatus.ControlledByContract) && (chunkGameLogic.GetState() == EncounterObjectStatus.Finished)) {
      return true;
    }
    return false;
  }
}