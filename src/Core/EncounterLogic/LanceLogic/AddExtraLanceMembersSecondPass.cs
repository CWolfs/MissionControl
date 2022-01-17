namespace MissionControl.Logic {
  public class AddExtraLanceMembersSecondPass : RequestLanceCompleteLogic {
    private LogicState state;

    public AddExtraLanceMembersSecondPass(LogicState state) {
      this.state = state;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddExtraLanceMembersSecondPass] Running second pass after LanceDef has resolved, if required");
    }
  }
}