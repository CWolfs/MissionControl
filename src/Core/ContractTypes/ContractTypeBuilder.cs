using BattleTech;
using BattleTech.Data;

namespace MissionControl.Logic {
  public class ContractTypeBuilder {
    private ContractTypeValue contractTypeValue;

    public ContractTypeBuilder(ContractTypeValue contractTypeValue) {
      this.contractTypeValue = contractTypeValue;


    }

    public void Run() {
      Main.LogDebug($"[{contractTypeValue.Name}] Building...");
    }
  }
}