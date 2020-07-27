using Newtonsoft.Json.Linq;

namespace MissionControl.Config {
  public class ContractSettingsOverrides {
    public static string AdditionalLances_Enable = "AdditionalLances.Enable";
    public static string AdditionalLances_AllyLanceCountOverride = "AdditionalLances.AllyLanceCount";
    public static string AdditionalLances_EnemyLanceCountOverride = "AdditionalLances.EnemyLanceCount";

    public static string ExtendedLances_Enable = "ExtendedLances.Enable";
    public static string ExtendedLances_AllyLanceSizeOverride = "ExtendedLances.AllyLanceSize";
    public static string ExtendedLances_EnemyLanceSizeOverride = "ExtendedLances.EnemyLanceSize";

    public static string RandomSpawns_Enable = "RandomSpawns.Enable";

    public static string DynamicWithdraw_Enable = "DynamicWithdraw.Enable";

    public static string HotDropProtection_Enable = "HotDropProtection.Enable";

    public static string ExtendedBoundaries_Enable = "ExtendedBoundaries.Enable";
    public static string ExtendedBoundaries_IncreaseBoundarySizeByPercentage = "ExtendedBoundaries.IncreaseBoundarySizeByPercentage";

    public static string AdditionalPlayerMechs_Enable = "AdditionalPlayerMechs.Enable";

    public bool Enabled {
      get => Properties != null;
    }
    public JObject Properties { get; set; }

    public bool Has(string path) {
      if (Properties == null) return false;
      JToken token = Properties.SelectToken(path);
      return token != null;
    }

    public bool GetBool(string path) {
      JToken token = Properties.SelectToken(path);
      return (bool)token;
    }

    public int GetInt(string path) {
      JToken token = Properties.SelectToken(path);
      return (int)token;
    }
  }
}