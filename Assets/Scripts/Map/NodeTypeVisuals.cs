using System;
using UnityEngine;

// Icon and tint for each node type, a plain circle is used when there is no icon
[CreateAssetMenu(menuName = "Map/Node Type Visuals")]
public class NodeTypeVisuals : ScriptableObject {
    [Serializable]
    public class Entry {
        public NodeType type;
        public Sprite icon;
        public Color tint = Color.white;
    }

    public Entry[] entries = {
        new Entry { type = NodeType.Level, tint = Color.white },
        new Entry { type = NodeType.Items, tint = Color.yellow },
        new Entry { type = NodeType.Discard, tint = new Color(0.7f, 0.3f, 0.9f) },
        new Entry { type = NodeType.Boss, tint = Color.red },
    };

    public Entry Get(NodeType type) {
        foreach (var entry in entries) {
            if (entry.type == type) return entry;
        }
        return null;
    }
}
