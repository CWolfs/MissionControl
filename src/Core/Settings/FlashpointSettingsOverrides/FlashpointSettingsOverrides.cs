using Newtonsoft.Json.Linq;

namespace MissionControl.Config {
  public class FlashpointSettingsOverrides {
    public JObject Properties { get; set; }

    public bool Is(string path) {
      JToken token = Properties.SelectToken(path);
      return (bool)token;
    }
  }
}