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

    [JsonProperty("Authors", Order = 4)]
    public List<string> Authors;

    [JsonProperty("Contributors", Order = 5)]
    public List<string> Contributors = new List<string>();

    public ContractTypeMetadata(string designerVersion, string minCompatibleMCVersion, string contractTypeVersion, string author, List<string> authors, List<string> contributors) {
      DesignerVersion = designerVersion;
      MinCompatibleMCVersion = minCompatibleMCVersion;
      ContractTypeVersion = contractTypeVersion;
      Author = author;
      Authors = authors;
      Contributors = contributors;
    }
  }
}