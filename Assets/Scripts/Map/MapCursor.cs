using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Keeps track of which available node is highlighted and confirms the choice
public class MapCursor : MonoBehaviour {
    private MapView view;
    private List<MapNode> options;
    private int index;
    private bool busy;

    void Start() {
        view = GetComponent<MapView>();
        RunManager.StateChanged += Rebuild;
        Rebuild();
    }

    void OnDestroy() {
        RunManager.StateChanged -= Rebuild;
    }

    public void Move(int direction) {
        if (busy) return;

        index = (index + direction + options.Count) % options.Count;
        view.Refresh(options[index]);
    }

    public void Confirm() {
        if (busy) return;
        StartCoroutine(ConfirmRoutine(options[index]));
    }

    private IEnumerator ConfirmRoutine(MapNode node) {
        busy = true;
        yield return view.MoveToken(node);
        RunManager.SelectNode(node);   // Items and Discard nodes call Rebuild through StateChanged
        busy = false;
    }

    private void Rebuild() {
        options = RunManager.State.AvailableNodes();
        index = 0;
        view.Refresh(options[0]);
    }
}
