using System.Collections.Generic;

[System.Serializable]
public class MapNode {
    public int id;
    public int row;                  // 0 = bottom
    public int column;               // used for layout
    public NodeType type;
    public List<int> next = new();   // ids of nodes in row + 1
    public LevelConfig level;        // set for Level and Boss nodes, null for the rest
}
