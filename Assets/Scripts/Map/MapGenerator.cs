using System;
using System.Collections.Generic;
using UnityEngine;

public static class MapGenerator {
    public static MapData Generate(RunConfig cfg, int seed) {
        var rng = new System.Random(seed);
        var map = new MapData { rowCount = cfg.rows };
        int bossRow = cfg.rows - 1;

        // Nodes, each row stays close to the one below so the edges stay short
        List<int> columns = null;
        for (int r = 0; r < bossRow; r++) {
            columns = PickColumns(cfg, rng, columns);
            foreach (int c in columns) AddNode(map, r, c);
        }
        AddNode(map, bossRow, (cfg.columns - 1) / 2);

        // Edges
        for (int r = 0; r < bossRow; r++) {
            Connect(map.Row(r), map.Row(r + 1));
        }

        // Types, a non-Level node can't come right after another non-Level node
        for (int r = 0; r <= bossRow; r++) {
            foreach (var n in map.Row(r)) {
                if (r == bossRow) n.type = NodeType.Boss;
                else if (r == 0 || r == bossRow - 1) n.type = NodeType.Level;
                else if (FollowsSpecialNode(map, n)) n.type = NodeType.Level;
                else n.type = RollType(cfg, rng);

                if (n.type == NodeType.Boss) n.level = cfg.bossLevel;
                else if (n.type == NodeType.Level && cfg.levels.Count > 0) n.level = cfg.levels[rng.Next(cfg.levels.Count)];
            }
        }

        Validate(map);
        return map;
    }

    private static void AddNode(MapData map, int row, int column) {
        map.nodes.Add(new MapNode { id = map.nodes.Count, row = row, column = column });
    }

    private static List<int> PickColumns(RunConfig cfg, System.Random rng, List<int> previous) {
        int count = Math.Min(rng.Next(cfg.minNodesPerRow, cfg.maxNodesPerRow + 1), cfg.columns);
        var picked = new List<int>();

        // Try a few times to get a first and last column close to the previous row's
        for (int attempt = 0; attempt < 10; attempt++) {
            picked.Clear();
            while (picked.Count < count) {
                int column = rng.Next(cfg.columns);
                if (!picked.Contains(column)) picked.Add(column);
            }
            picked.Sort();

            if (previous == null) break;
            bool firstIsClose = Math.Abs(picked[0] - previous[0]) <= 1;
            bool lastIsClose = Math.Abs(picked[picked.Count - 1] - previous[previous.Count - 1]) <= 1;
            if (firstIsClose && lastIsClose) break;
        }
        return picked;
    }

    // Walks along both rows from left to right and links the nodes it stands on
    // Every node ends up with an edge in and out, and no edges cross
    private static void Connect(List<MapNode> lower, List<MapNode> upper) {
        int i = 0;
        int j = 0;
        while (true) {
            lower[i].next.Add(upper[j].id);
            if (i == lower.Count - 1 && j == upper.Count - 1) break;

            // Step forward on the side that gives the shorter next edge
            bool stepLower;
            if (i == lower.Count - 1) stepLower = false;
            else if (j == upper.Count - 1) stepLower = true;
            else {
                int lowerStepDistance = Math.Abs(lower[i + 1].column - upper[j].column);
                int upperStepDistance = Math.Abs(lower[i].column - upper[j + 1].column);
                stepLower = lowerStepDistance <= upperStepDistance;
            }

            if (stepLower) i++;
            else j++;
        }
    }

    private static bool FollowsSpecialNode(MapData map, MapNode n) {
        foreach (var below in map.Row(n.row - 1)) {
            if (below.type != NodeType.Level && below.next.Contains(n.id)) return true;
        }
        return false;
    }

    private static NodeType RollType(RunConfig cfg, System.Random rng) {
        double roll = rng.NextDouble() * (cfg.levelWeight + cfg.itemsWeight + cfg.discardWeight);
        if (roll < cfg.levelWeight) return NodeType.Level;
        if (roll < cfg.levelWeight + cfg.itemsWeight) return NodeType.Items;
        return NodeType.Discard;
    }

    // Edges only go up and the last row is a single boss node, so if every node
    // has an edge out, the boss can be reached from every starting node
    private static void Validate(MapData map) {
        foreach (var n in map.nodes) {
            if (n.type != NodeType.Boss && n.next.Count == 0) {
                Debug.LogError($"Map node {n.id} has no edge out");
            }
            if (n.row > 0 && !HasEdgeIn(map, n)) {
                Debug.LogError($"Map node {n.id} has no edge in");
            }
        }
    }

    private static bool HasEdgeIn(MapData map, MapNode n) {
        foreach (var below in map.Row(n.row - 1)) {
            if (below.next.Contains(n.id)) return true;
        }
        return false;
    }
}
