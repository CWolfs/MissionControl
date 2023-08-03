using BattleTech.Framework;

namespace MissionControl.Logic {
  public class ContractAndLanceOverridePayload : RunPayload {
    public ContractOverride ContractOverride { get; private set; }
    public LanceOverride LanceOverride { get; private set; }

    public ContractAndLanceOverridePayload(ContractOverride contractOverride, LanceOverride lanceOverride) {
      ContractOverride = contractOverride;
      LanceOverride = lanceOverride;
    }
  }
}