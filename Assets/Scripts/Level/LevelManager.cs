using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour {
    public enum LevelState { Playing, Won, Lost }

    public static LevelManager Instance { get; private set; }

    [SerializeField] private float resultDelay = 2f;
    [SerializeField] private float startGracePeriod = 0.5f;

    public LevelState State { get; private set; } = LevelState.Playing;
    public bool IsInGracePeriod => Time.time < startTime + startGracePeriod;

    private float startTime;

    void Awake() {
        Instance = this;
        startTime = Time.time;
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    public void Complete() {
        if (State != LevelState.Playing) return;
        State = LevelState.Won;
        FreezeShip();
        HandleResult(true);
    }

    public void Fail() {
        if (State != LevelState.Playing) return;
        State = LevelState.Lost;
        FreezeShip();
        HandleResult(false);
    }

    private void FreezeShip() {
        var ship = FindAnyObjectByType<PlayerAndSpaceshipSync>();
        if (ship == null || !ship.TryGetComponent(out Rigidbody2D rb)) return;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
    }

    // Placeholder result handling: just reload the scene.
    private void HandleResult(bool won) {
        StartCoroutine(ReloadAfterDelay());
    }

    private IEnumerator ReloadAfterDelay() {
        yield return new WaitForSecondsRealtime(resultDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnGUI() {
        if (State == LevelState.Playing) return;
        bool won = State == LevelState.Won;
        var style = new GUIStyle(GUI.skin.label) {
            fontSize = 48,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = won ? Color.green : Color.red;
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), won ? "LEVEL COMPLETE" : "SHIP DESTROYED", style);
    }
}
