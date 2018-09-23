using BattleTech.Framework;

namespace ContractCommand.Logic {
  public class ContractOverridePayload : RunPayload {
    public ContractOverride ContractOverride { get; private set; }

    public ContractOverridePayload(ContractOverride contractOverride) {
      ContractOverride = contractOverride;
    }
  }
}