using UnityEngine;

[CreateAssetMenu(menuName = "Run/Level Config")]
public class LevelConfig : ScriptableObject {
    public string displayName;
    public GameObject layoutPrefab;
    public int difficulty;
}
