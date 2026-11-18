using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Owns the current run, it is static so it survives scene loads
public static class RunManager {
    public static RunState State { get; private set; }

    // Called when the run changes without a scene load (picking an Items or Discard node)
    public static event Action StateChanged;

    private static RunConfig config;
    private static bool? lastRunWon;

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
        lastRunWon = null;
        StateChanged = null;
    }

    public static void StartNewRun() {
        CreateRun();
        SceneManager.LoadScene(SceneNames.Map);
    }

    public static void CreateRun() {
        int seed = Config.seed != 0 ? Config.seed : new System.Random().Next(1, int.MaxValue);
        State = new RunState { seed = seed, map = MapGenerator.Generate(Config, seed) };
    }

    public static bool CanSelect(MapNode n) {
        return State.PendingNode == null && State.AvailableNodes().Contains(n);
    }

    public static void SelectNode(MapNode n) {
        if (!CanSelect(n)) return;

        if (n.level != null) {
            State.PendingNode = n;
            SceneManager.LoadScene(SceneNames.Gameplay);
        } else {
            // Items and Discard nodes have no content yet, so they are done right away
            State.Visit(n);
            StateChanged?.Invoke();
        }
    }

    // Only called from the level scene
    public static void CompletePendingNode() {
        var node = State.PendingNode;
        State.PendingNode = null;
        State.Visit(node);

        if (node.type == NodeType.Boss) EndRun(true);
        else SceneManager.LoadScene(SceneNames.Map);
    }

    public static void FailRun() {
        EndRun(false);
    }

    // Result of the run that just ended, shown once. Null when there is nothing to show
    public static bool? TakeLastResult() {
        bool? result = lastRunWon;
        lastRunWon = null;
        return result;
    }

    private static void EndRun(bool won) {
        lastRunWon = won;
        StartNewRun();
    }
}
