using System.Collections.Generic;

public class MapData {
    public List<MapNode> nodes = new();
    public List<List<MapNode>> rows = new();   // rows[0] is the bottom row, each row is ordered by column

    public int RowCount => rows.Count;

    // Node ids are indices into the list
    public MapNode Get(int id) {
        return nodes[id];
    }

    public List<MapNode> Row(int row) {
        return rows[row];
    }
}
