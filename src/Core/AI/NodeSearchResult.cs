using System.Collections.Generic;

using BattleTech;

namespace MissionControl.AI {
  public class NodeSearchResult {
    public CompositeBehaviorNode ParentNode { get; private set; }
    public List<BehaviorNode> NodeSiblings { get; private set; }
    public int NodeIndex { get; private set; }
    public BehaviorNode Node { get; private set; }

    public NodeSearchResult(CompositeBehaviorNode parentNode, List<BehaviorNode> nodeSiblings, int nodeIndex, BehaviorNode node) {
      this.ParentNode = parentNode;
      this.NodeSiblings = nodeSiblings;
      this.NodeIndex = nodeIndex;
      this.Node = node;
    }
  }
}