using System.Collections;
using UnityEngine;

public class LevelManager : MonoBehaviour {
    public enum LevelState { Playing, Won, Lost }

    public static LevelManager Instance { get; private set; }

    [SerializeField] private Rigidbody2D ship;
    [SerializeField] private float resultDelay = 2f;
    [SerializeField] private float startGracePeriod = 0.5f;

    public Rigidbody2D Ship => ship;
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
        End(LevelState.Won);
    }

    public void Fail() {
        End(LevelState.Lost);
    }

    private void End(LevelState result) {
        if (State != LevelState.Playing) return;
        State = result;

        // Freeze the ship
        ship.linearVelocity = Vector2.zero;
        ship.angularVelocity = 0f;
        ship.simulated = false;

        StartCoroutine(FinishAfterDelay());
    }

    private IEnumerator FinishAfterDelay() {
        yield return new WaitForSecondsRealtime(resultDelay);

        if (State == LevelState.Won) RunManager.CompletePendingNode();
        else RunManager.FailRun();
    }

    void OnGUI() {
        if (State == LevelState.Playing) return;

        bool won = State == LevelState.Won;
        DebugText.Draw(won ? "LEVEL COMPLETE" : "SHIP DESTROYED", 48, won ? Color.green : Color.red, TextAnchor.MiddleCenter);
    }
}
