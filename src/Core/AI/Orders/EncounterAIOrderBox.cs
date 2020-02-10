using BattleTech;

namespace MissionControl.AI {
  public class EncounterAIOrderBox : AIOrderBox {
    public EncounterAIOrderBox() { }

    public EncounterAIOrderBox(AIOrder order) {
      this.CargoVTwo = order;
    }
  }
}