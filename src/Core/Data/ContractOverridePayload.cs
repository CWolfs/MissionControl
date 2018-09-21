using BattleTech.Framework;

namespace SpawnVariation.Logic {
  public class ContractOverridePayload : RunPayload {
    public ContractOverride ContractOverride { get; private set; }

    public ContractOverridePayload(ContractOverride contractOverride) {
      ContractOverride = contractOverride;
    }
  }
}