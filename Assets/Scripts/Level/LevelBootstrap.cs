using UnityEngine;

// Loads the layout of the level the player picked on the map and puts the ship at its start
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(LevelManager))]
public class LevelBootstrap : MonoBehaviour {
    [SerializeField] private float nameShowTime = 2f;

    private string levelName;

    void Awake() {
        var level = RunManager.State.PendingNode.level;
        var layout = Instantiate(level.layoutPrefab).GetComponent<LevelLayout>();

        // Move the ship before the physics run
        var ship = GetComponent<LevelManager>().Ship;
        ship.transform.SetPositionAndRotation(layout.shipStart.position, layout.shipStart.rotation);

        levelName = level.displayName;
    }

    void OnGUI() {
        if (Time.timeSinceLevelLoad > nameShowTime) return;

        DebugText.Draw(levelName, 36, Color.white, TextAnchor.UpperCenter);
    }
}
