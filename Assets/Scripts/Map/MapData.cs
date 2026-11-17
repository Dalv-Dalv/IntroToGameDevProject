using System.Collections.Generic;

public class MapData {
    public List<MapNode> nodes = new();
    public int rowCount;

    // Node ids are indices into the list
    public MapNode Get(int id) {
        return nodes[id];
    }

    public List<MapNode> Row(int row) {
        var result = new List<MapNode>();
        foreach (var n in nodes) {
            if (n.row == row) result.Add(n);
        }
        return result;
    }
}
