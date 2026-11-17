using UnityEngine;
using UnityEngine.SceneManagement;

// Owns the current run, it is static so it survives scene loads
public static class RunManager {
    public static RunState State { get; private set; }
    public static bool HasActiveRun => State != null;

    // Result of the run that just ended, null when there is nothing to show
    public static bool? LastRunWon { get; set; }

    private static RunConfig config;

    public static RunConfig Config {
        get {
            if (config == null) config = Resources.Load<RunConfig>("RunConfig");
            return config;
        }
    }

    // Statics are not reset when domain reload is disabled for play mode, so clear them on start
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() {
        State = null;
        LastRunWon = null;
    }

    public static void StartNewRun() {
        CreateRun();
        SceneManager.LoadScene(SceneNames.Map);
    }

    private static void CreateRun() {
        int seed = Config.seed != 0 ? Config.seed : new System.Random().Next(1, int.MaxValue);
        State = new RunState { seed = seed, map = MapGenerator.Generate(Config, seed) };
    }

    public static bool CanSelect(MapNode n) {
        if (State == null || State.PendingNode != null) return false;
        return State.AvailableNodes().Contains(n);
    }

    public static void SelectNode(MapNode n) {
        if (!CanSelect(n)) return;
        State.PendingNode = n;

        if (n.type == NodeType.Level || n.type == NodeType.Boss) {
            SceneManager.LoadScene(SceneNames.Gameplay);
        } else {
            // Items and Discard nodes have no content yet, so they complete right away
            CompletePendingNode();
        }
    }

    public static void CompletePendingNode() {
        var node = State.PendingNode;
        State.visitedNodeIds.Add(node.id);
        State.currentNodeId = node.id;
        State.PendingNode = null;

        if (node.type == NodeType.Boss) EndRun(true);
        else if (SceneManager.GetActiveScene().name != SceneNames.Map) SceneManager.LoadScene(SceneNames.Map);
    }

    public static void FailRun() {
        EndRun(false);
    }

    private static void EndRun(bool won) {
        LastRunWon = won;
        State = null;
        StartNewRun();
    }
}
