using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Data {
  public class ContractTypeMetadata {
    [JsonProperty("DesignerVersion", Order = 0)]
    public string DesignerVersion;

    [JsonProperty("MinCompatibleMCVersion", Order = 1)]
    public string MinCompatibleMCVersion;

    [JsonProperty("ContractTypeVersion", Order = 2)]
    public string ContractTypeVersion;

    [JsonProperty("Author", Order = 3)]
    public string Author;

    [JsonProperty("Contributors", Order = 4)]
    public List<string> Contributors = new List<string>();

    public ContractTypeMetadata(string designerVersion, string minCompatibleMCVersion, string contractTypeVersion, string author, List<string> contributors) {
      DesignerVersion = designerVersion;
      MinCompatibleMCVersion = minCompatibleMCVersion;
      ContractTypeVersion = contractTypeVersion;
      Author = author;
      Contributors = contributors;
    }
  }
}