using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Keeps track of which available node is highlighted and confirms the choice
public class MapCursor : MonoBehaviour {
    private MapView view;
    private List<MapNode> options = new();
    private int index;
    private bool busy;

    void Start() {
        view = GetComponent<MapView>();
        Rebuild();
    }

    public void Move(int direction) {
        if (busy || options.Count == 0) return;

        index = (index + direction + options.Count) % options.Count;
        UpdateSelection();
    }

    public void Confirm() {
        if (busy || options.Count == 0) return;
        StartCoroutine(ConfirmRoutine(options[index]));
    }

    private IEnumerator ConfirmRoutine(MapNode node) {
        busy = true;
        yield return view.MoveToken(node);
        RunManager.SelectNode(node);

        // Items and Discard nodes finish right away, level nodes load another scene
        var state = RunManager.State;
        if (state != null && state.PendingNode == null) {
            busy = false;
            Rebuild();
        }
    }

    private void Rebuild() {
        options = RunManager.State.AvailableNodes();
        options.Sort((a, b) => a.column.CompareTo(b.column));
        index = 0;
        UpdateSelection();
    }

    private void UpdateSelection() {
        view.Refresh();
        if (options.Count > 0) view.GetView(options[index].id).SetState(MapNodeView.NodeState.Selected);
    }
}
