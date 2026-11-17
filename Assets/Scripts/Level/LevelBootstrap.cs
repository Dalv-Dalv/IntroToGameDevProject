using UnityEngine;

// Loads the layout of the level the player picked on the map
// Without a run (scene played directly) the default layout stays
[DefaultExecutionOrder(-100)]
public class LevelBootstrap : MonoBehaviour {
    [SerializeField] private GameObject defaultLayout;
    [SerializeField] private float nameShowTime = 2f;

    private string levelName;

    void Awake() {
        if (!RunManager.HasActiveRun) return;

        var node = RunManager.State.PendingNode;
        if (node == null || node.level == null || node.level.layoutPrefab == null) return;
        var level = node.level;

        if (defaultLayout != null) defaultLayout.SetActive(false);
        var layout = Instantiate(level.layoutPrefab).GetComponent<LevelLayout>();

        // Move the ship before the physics run
        var ship = FindAnyObjectByType<PlayerAndSpaceshipSync>();
        ship.transform.SetPositionAndRotation(layout.shipStart.position, layout.shipStart.rotation);

        levelName = level.displayName;
    }

    void OnGUI() {
        if (levelName == null || Time.timeSinceLevelLoad > nameShowTime) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.UpperCenter };
        GUI.Label(new Rect(0, 40, Screen.width, 100), levelName, style);
    }
}
