namespace MissionControl.Data {
  public class BranchInjectionData  {
    public string[] Path = new string[0];
    public CompositeBehaviorNode BranchRoot;

    public BranchInjectionData(string[] path, CompositeBehaviorNode compositeNode) {
      this.Path = path;
      this.BranchRoot = compositeNode;
    }
  }
}