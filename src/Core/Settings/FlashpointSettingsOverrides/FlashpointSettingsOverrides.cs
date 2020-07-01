using Newtonsoft.Json.Linq;

namespace MissionControl.Config {
  public class FlashpointSettingsOverrides {
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