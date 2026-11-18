using UnityEngine;

// Shows the result of the previous run for a few seconds
public class RunResultScreen : MonoBehaviour {
    [SerializeField] private float showTime = 3f;

    private bool? won;

    void Start() {
        won = RunManager.TakeLastResult();
    }

    void OnGUI() {
        if (won == null || Time.timeSinceLevelLoad > showTime) return;

        DebugText.Draw(won.Value ? "RUN COMPLETE" : "RUN FAILED", 48, won.Value ? Color.green : Color.red, TextAnchor.MiddleCenter);
    }
}
