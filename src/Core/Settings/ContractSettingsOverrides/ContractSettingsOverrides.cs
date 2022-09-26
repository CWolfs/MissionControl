using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace MissionControl.Config {
  public class ContractSettingsOverrides {
    public static string AdditionalLances_Enable = "AdditionalLances.Enable";
    public static string AdditionalLances_AllyLanceCountOverride = "AdditionalLances.AllyLanceCount";
    public static string AdditionalLances_EnemyLanceCountOverride = "AdditionalLances.EnemyLanceCount";
    public static string AdditionalLances_AllyLancesOverride = "AdditionalLances.AllyLances";
    public static string AdditionalLances_EnemyLancesOverride = "AdditionalLances.EnemyLances";
    public static string AdditionalLances_EnemyLanceObjectiveNamesOverride = "AdditionalLances.EnemyLanceObjectiveNames";

    public static string ExtendedLances_Enable = "ExtendedLances.Enable";

    // ELv1
    public static string ExtendedLances_AllyLanceSizeOverride = "ExtendedLances.AllyLanceSize";
    public static string ExtendedLances_EnemyLanceSizeOverride = "ExtendedLances.EnemyLanceSize";

    // ELv2
    public static string ExtendedLances_EmployerLanceSizeOverride = "ExtendedLances.EmployerLanceSize";
    public static string ExtendedLances_TargetLanceSizeOverride = "ExtendedLances.TargetLanceSize";
    public static string ExtendedLances_TargetAllyLanceSizeOverride = "ExtendedLances.TargetAllyLanceSize";
    public static string ExtendedLances_EmployerAllyLanceSizeOverride = "ExtendedLances.EmployerAllyLanceSize";
    public static string ExtendedLances_HostileToAllLanceSizeOverride = "ExtendedLances.HostileToAllLanceSize";
    public static string ExtendedLances_NeutralToAllLanceSizeOverride = "ExtendedLances.NeutralToAllLanceSize";

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

    public List<T> GetList<T>(string path) {
      JToken token = Properties.SelectToken(path);
      return token.ToObject<List<T>>();
    }
  }
}