using UnityEngine;

public class MapNodeView : MonoBehaviour {
    public enum NodeState { Locked, Visited, Available, Selected }

    public MapNode Node { get; private set; }

    private SpriteRenderer sr;
    private Color tint;
    private float baseScale;
    private NodeState state;

    public void Init(MapNode node, SpriteRenderer renderer, Color nodeTint, float scale) {
        Node = node;
        sr = renderer;
        tint = nodeTint;
        baseScale = scale;
    }

    public void SetState(NodeState newState) {
        state = newState;

        if (state == NodeState.Locked) sr.color = new Color(0.6f, 0.55f, 0.45f);
        else if (state == NodeState.Visited) sr.color = tint * 0.5f;
        else sr.color = tint;
    }

    void Update() {
        float scale = baseScale;
        if (state == NodeState.Selected) scale *= 1.3f + 0.05f * Mathf.Sin(Time.time * 6f);
        else if (state == NodeState.Available) scale *= 1f + 0.06f * Mathf.Sin(Time.time * 2f);
        transform.localScale = Vector3.one * scale;
    }
}
