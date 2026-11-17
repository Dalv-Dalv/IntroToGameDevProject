using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Run/Run Config")]
public class RunConfig : ScriptableObject {
    [Header("Map size")]
    public int rows = 7;
    public int minNodesPerRow = 2;
    public int maxNodesPerRow = 3;
    public int columns = 4;

    [Header("Node type weights")]
    public float levelWeight = 6f;
    public float itemsWeight = 2f;
    public float discardWeight = 1f;

    [Header("Run")]
    [Tooltip("0 = random seed each run")]
    public int seed = 0;
    public List<LevelConfig> levels = new();
    public LevelConfig bossLevel;
}
