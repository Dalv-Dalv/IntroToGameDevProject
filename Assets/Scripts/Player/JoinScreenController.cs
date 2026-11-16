using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Join screen: shows who joined and starts the game when any joined player presses Interact
public class JoinScreenController : MonoBehaviour {
    void Update() {
        if (PlayerRoster.Count == 0) return;

        foreach (var player in PlayerInput.all) {
            if (player.actions["Interact"].WasPressedThisFrame()) {
                SceneManager.LoadScene(SceneNames.Gameplay);
                return;
            }
        }
    }

    void OnGUI() {
        string text = "Press Start / Space / Enter / RShift / NumpadEnter to join\n\n";
        for (int i = 0; i < PlayerRoster.Count; i++) {
            text += $"Player {i + 1}: {PlayerRoster.Entries[i].controlScheme}\n";
        }
        if (PlayerRoster.Count > 0) text += "\nInteract to start";

        var style = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), text, style);
    }
}
