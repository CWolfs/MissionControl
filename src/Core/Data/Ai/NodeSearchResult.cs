using System.Collections.Generic;

using BattleTech;

namespace MissionControl.Data {
  public class NodeSearchResult {
    public List<BehaviorNode> NodeSiblings { get; private set; }
    public int NodeIndex { get; private set; }
    public BehaviorNode Node { get; private set; }

    public NodeSearchResult(List<BehaviorNode> nodeSiblings, int nodeIndex, BehaviorNode node) {
      this.NodeSiblings = nodeSiblings;
      this.NodeIndex = nodeIndex;
      this.Node = node;
    }
  }
}