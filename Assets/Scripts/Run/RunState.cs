using System.Collections.Generic;

public class RunState {
    public int seed;
    public MapData map;
    public int currentNodeId = -1;   // -1 means the run just started
    public List<int> visitedNodeIds = new();
    public MapNode PendingNode;      // level node being played right now

    // Row 0 at the start, otherwise the nodes connected to the current one
    // Only these can be picked, so there is no going back
    public List<MapNode> AvailableNodes() {
        if (currentNodeId == -1) return map.Row(0);

        var result = new List<MapNode>();
        foreach (int id in map.Get(currentNodeId).next) {
            result.Add(map.Get(id));
        }
        return result;
    }

    public void Visit(MapNode node) {
        visitedNodeIds.Add(node.id);
        currentNodeId = node.id;
    }
}
