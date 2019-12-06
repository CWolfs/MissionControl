using Newtonsoft.Json.Linq;

namespace MissionControl.Logic {
  public class ContractTypeBuilder {
    private JObject contractTypeBuild;
    private string contractTypeKey = "UNSET";

    private const string CHUNKS_ID = "Chunks";

    public ContractTypeBuilder(JObject contractTypeBuild) {
      this.contractTypeBuild = contractTypeBuild;
      contractTypeKey = contractTypeBuild["Key"].ToString();
    }

    public bool Build() {
      Main.LogDebug($"[ContractTypeBuild] Building '{contractTypeKey}'");

      BuildChunks();

      return true;
    }

    private void BuildChunks() {
      if (contractTypeBuild.ContainsKey(CHUNKS_ID)) {
        JArray chunksArray = (JArray)contractTypeBuild[CHUNKS_ID];
        Main.LogDebug($"[ContractTypeBuild.{contractTypeKey}] There are '{chunksArray.Count} chunk(s) defined.'");
      }
    }
  }
}