namespace MissionControl.Config {
  public class Metrics {
    public int NumberOfAdditionalLances {
      get {
        return NumberOfEmployerAdditionalLances + NumberOfTargetAdditionalLances;
      }
    }

    public int NumberOfEmployerAdditionalLances { get; set; } = 0;
    public int NumberOfTargetAdditionalLances { get; set; } = 0;
  }
}