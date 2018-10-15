using BattleTech.Framework;

using MissionControl.Logic;

namespace MissionControl.Result {
  public class EncounterResultBox : DesignResultBox {
    public EncounterResultBox() { }

    public EncounterResultBox(DesignResult designResult) {
      this.CargoVTwo = designResult;
    }
  }
}