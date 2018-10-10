using System;
using BattleTech;

using MissionControl;

public class LogMessageSuccess : LeafBehaviorNode {
	private string logMessage;

	public LogMessageSuccess(string name, BehaviorTree tree, AbstractActor unit, string msg) : base(name, tree, unit) {
		this.logMessage = msg;
	}

	protected override BehaviorTreeResults Tick() {
		MissionControl.Main.Logger.Log("Test");
		return new BehaviorTreeResults(BehaviorNodeState.Success);
	}
}
