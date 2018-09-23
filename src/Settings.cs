namespace EncounterCommand {
    public class Settings {
        public int MinNumberOfAdditionalEnemyLances { get; set; } = 0;
        public int MaxNumberOfAdditionalEnemyLances { get; set; } = 0;

        public int SelectNumberOfAdditionalEnemyLances() {
            int lanceNumber = UnityEngine.Random.Range(MinNumberOfAdditionalEnemyLances, MaxNumberOfAdditionalEnemyLances);
            Main.Logger.Log("[SelectNumberOfAdditionalEnemyLances] {lanceNumber}");
            return lanceNumber;
        }
    }
}