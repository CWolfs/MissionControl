using System;
using BattleTech;

using Harmony;

using MissionControl;

namespace MissionControl.AI {
	public class LogMessageNode : LeafBehaviorNode {
		private string logMessage;
		private BehaviorNodeState requestedState;

		public LogMessageNode(string name, BehaviorTree tree, AbstractActor unit, string msg, BehaviorNodeState requestedState) : base(name, tree, unit) {
			this.logMessage = msg;
			this.requestedState = requestedState;
		}

		protected override BehaviorTreeResults Tick() {
			int currentRound = tree.battleTechGame.Combat.TurnDirector.CurrentRound;
			int currentPhase = tree.battleTechGame.Combat.TurnDirector.CurrentPhase;
			int actionCount = (AiUtils.IsOnFirstAction(unit)) ? 1 : 2;

			BehaviorTreeIDEnum behaviorTreeType = (BehaviorTreeIDEnum)AccessTools.Field(typeof(BehaviorTree), "behaviorTreeIDEnum").GetValue(this.tree);

			Main.Logger.Log($"[AI] [Round '{currentRound}' Phase '{currentPhase}' Unit Action '{actionCount}'] ['{unit.team.Name}' unit '{unit.DisplayName}' AI type '{behaviorTreeType}'] {this.logMessage}");
			return new BehaviorTreeResults(requestedState);
		}
	}
}