namespace MissionControl.Data {
  public class ReservedIdEntry {
    public string ModName { get; set; }
    public string Id { get; set; }
    public string ContractTypeName { get; set; }

    public ReservedIdEntry(string modName, string id, string contractTypeName) {
      ModName = modName;
      Id = id;
      ContractTypeName = contractTypeName;
    }

    public override string ToString() {
      return $"{ModName}: ID {Id} -> {ContractTypeName}";
    }
  }
}
