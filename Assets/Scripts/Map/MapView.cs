using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Builds the flat 2D map from the current run: background, dashed edges, nodes and the ship token
public class MapView : MonoBehaviour {
    [Header("Layout")]
    [SerializeField] private float xSpacing = 2.5f;
    [SerializeField] private float ySpacing = 2f;
    [SerializeField] private float jitter = 0.3f;
    [SerializeField] private float dashSpacing = 0.4f;

    [Header("Visuals")]
    [SerializeField] private NodeTypeVisuals visuals;
    [SerializeField] private Color backgroundColor = new Color(0.85f, 0.78f, 0.6f);
    [SerializeField] private Color edgeColor = new Color(0.3f, 0.2f, 0.1f);
    [SerializeField] private float tokenMoveTime = 0.5f;

    private RunState state;
    private readonly Dictionary<int, Vector3> positions = new();
    private readonly Dictionary<int, MapNodeView> nodeViews = new();
    private readonly List<MapEdge> edges = new();
    private Transform token;

    void Awake() {
        state = RunManager.State;

        if (visuals == null) visuals = ScriptableObject.CreateInstance<NodeTypeVisuals>();

        CalculatePositions(RunManager.Config.columns);
        CreateBackground();
        CreateEdges();
        CreateNodes();
        CreateToken();

        var mapCamera = FindAnyObjectByType<MapCamera>();
        if (mapCamera != null) mapCamera.SetTarget(token);
    }

    // Updates how the nodes and edges look, selected is the node the cursor is on
    public void Refresh(MapNode selected) {
        var available = state.AvailableNodes();

        var reachable = new HashSet<int>();
        foreach (var node in available) AddReachable(node, reachable);

        foreach (var view in nodeViews.Values) {
            if (view.Node == selected) view.SetState(MapNodeView.NodeState.Selected);
            else if (state.visitedNodeIds.Contains(view.Node.id)) view.SetState(MapNodeView.NodeState.Visited);
            else if (available.Contains(view.Node)) view.SetState(MapNodeView.NodeState.Available);
            else view.SetState(MapNodeView.NodeState.Locked);
        }

        foreach (var edge in edges) {
            bool live = edge.From.id == state.currentNodeId || reachable.Contains(edge.From.id);
            edge.SetLive(live);
        }

        token.position = TokenHome();
    }

    public IEnumerator MoveToken(MapNode node) {
        Vector3 start = token.position;
        Vector3 end = positions[node.id];

        for (float t = 0f; t < 1f; t += Time.deltaTime / tokenMoveTime) {
            token.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        token.position = end;
    }

    // Adds a node and every node above it that can be reached from it
    private void AddReachable(MapNode node, HashSet<int> reachable) {
        if (!reachable.Add(node.id)) return;
        foreach (int nextId in node.next) AddReachable(state.map.Get(nextId), reachable);
    }

    private void CalculatePositions(int columns) {
        var rng = new System.Random(state.seed);

        foreach (var node in state.map.nodes) {
            float x = (node.column - (columns - 1) / 2f) * xSpacing + RandomOffset(rng);
            float y = node.row * ySpacing + RandomOffset(rng);
            positions[node.id] = new Vector3(x, y, 0f);
        }
    }

    private float RandomOffset(System.Random rng) {
        return ((float)rng.NextDouble() * 2f - 1f) * jitter;
    }

    private void CreateBackground() {
        float centerY = (state.map.RowCount - 1) * ySpacing / 2f;
        var background = NewSprite("Background", MapSprites.Square(), -10, new Vector3(0f, centerY, 0f));
        background.color = backgroundColor;
        background.transform.localScale = new Vector3(40f, state.map.RowCount * ySpacing + 30f, 1f);
    }

    private void CreateEdges() {
        foreach (var node in state.map.nodes) {
            foreach (int nextId in node.next) {
                var edge = new GameObject("Edge").AddComponent<MapEdge>();
                edge.transform.SetParent(transform);
                edge.Init(node, positions[node.id], positions[nextId], dashSpacing, edgeColor);
                edges.Add(edge);
            }
        }
    }

    private void CreateNodes() {
        foreach (var node in state.map.nodes) {
            Sprite sprite = MapSprites.Circle();
            Color tint = Color.white;

            var entry = visuals.Get(node.type);
            if (entry != null) {
                tint = entry.tint;
                if (entry.icon != null) sprite = entry.icon;
            }

            var sr = NewSprite("Node " + node.id, sprite, 1, positions[node.id]);
            float scale = node.type == NodeType.Boss ? 1.4f : 1f;

            var view = sr.gameObject.AddComponent<MapNodeView>();
            view.Init(node, sr, tint, scale);
            nodeViews[node.id] = view;
        }
    }

    // The token sits on the current node, or below row 0 at the start of a run
    private Vector3 TokenHome() {
        if (state.currentNodeId == -1) return new Vector3(0f, -ySpacing, 0f);
        return positions[state.currentNodeId];
    }

    private void CreateToken() {
        var sr = NewSprite("Ship Token", MapSprites.Circle(), 3, TokenHome());
        sr.color = Color.cyan;
        sr.transform.localScale = Vector3.one * 0.4f;
        token = sr.transform;
    }

    private SpriteRenderer NewSprite(string objectName, Sprite sprite, int sortingOrder, Vector3 position) {
        var sr = new GameObject(objectName).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(transform);
        sr.transform.position = position;
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        return sr;
    }
}
