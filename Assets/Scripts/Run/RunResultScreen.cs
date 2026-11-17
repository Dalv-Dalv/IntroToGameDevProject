using UnityEngine;

// Shows the result of the previous run for a few seconds
public class RunResultScreen : MonoBehaviour {
    [SerializeField] private float showTime = 3f;

    private float timer;

    void Update() {
        if (RunManager.LastRunWon == null) return;

        timer += Time.deltaTime;
        if (timer > showTime) {
            RunManager.LastRunWon = null;
            timer = 0f;
        }
    }

    void OnGUI() {
        if (RunManager.LastRunWon == null) return;

        bool won = RunManager.LastRunWon.Value;
        var style = new GUIStyle(GUI.skin.label) {
            fontSize = 48,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = won ? Color.green : Color.red;
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), won ? "RUN COMPLETE" : "RUN FAILED", style);
    }
}
