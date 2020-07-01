using Newtonsoft.Json.Linq;

namespace MissionControl.Config {
  public class FlashpointSettingsOverrides {
    public static string AdditionalLances_Enable = "AdditionalLances.Enable";
    public static string AdditionalLances_AllyLanceCountOverride = "AdditionalLances.AllyLanceCountOverride";
    public static string AdditionalLances_EnemyLanceCountOverride = "AdditionalLances.EnemyLanceCountOverride";

    public static string ExtendedLances_Enable = "ExtendedLances.Enable";
    public static string ExtendedLances_AllyLanceSizeOverride = "ExtendedLances.AllyLanceSizeOverride";
    public static string ExtendedLances_EnemyLanceSizeOverride = "ExtendedLances.EnemyLanceSizeOverride";

    public static string RandomSpawns_Enable = "RandomSpawns.Enable";

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