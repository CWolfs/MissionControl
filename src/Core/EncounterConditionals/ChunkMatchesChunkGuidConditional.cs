using BattleTech.Framework;

using MissionControl.Messages;

namespace MissionControl.Conditional {
  public class ChunkMatchesChunkGuidConditional : DesignConditional {
    public string ChunkGuid { get; set; }

    public override bool Evaluate(MessageCenterMessage message, string responseName) {
      base.Evaluate(message, responseName);
      ChunkMessage chunkMessage = message as ChunkMessage;
      
      Main.LogDebug("[ChunkMatchesChunkGuidConditional] Evaluating...");

			if (chunkMessage != null && chunkMessage.ChunkGuid == this.ChunkGuid) {
				base.LogEvaluationPassed("Chunk matches guid of message.", responseName);
				return true;
			}
			base.LogEvaluationFailed("Chunk did NOT match guid of message.", responseName);
			return false;
    }
  }
}