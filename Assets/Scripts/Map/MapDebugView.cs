using UnityEngine;

// Text-only map: rows from the boss (top) down to row 0. Available nodes are buttons
public class MapDebugView : MonoBehaviour {
    void OnGUI() {
        if (!RunManager.HasActiveRun) {
            GUILayout.Label("No active run");
            return;
        }

        var state = RunManager.State;

        GUILayout.Label($"Seed {state.seed}");
        for (int r = state.map.rowCount - 1; r >= 0; r--) {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Row {r}", GUILayout.Width(50));
            foreach (var n in state.map.Row(r)) {
                GUI.enabled = RunManager.CanSelect(n);
                string status = n.id == state.currentNodeId ? "CURRENT"
                    : state.visitedNodeIds.Contains(n.id) ? "visited"
                    : GUI.enabled ? "available" : "locked";
                string label = $"{n.type} (col {n.column})\n{status}\n-> {string.Join(",", n.next)}";
                if (GUILayout.Button(label, GUILayout.Width(130), GUILayout.Height(50))) RunManager.SelectNode(n);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }
    }
}
