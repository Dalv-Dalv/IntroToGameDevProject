using UnityEngine;
using UnityEngine.InputSystem;

// Join screen: shows who joined and starts the run when any joined player presses Interact
public class JoinScreenController : MonoBehaviour {
    void Update() {
        if (PlayerRoster.Count == 0) return;

        foreach (var player in PlayerInput.all) {
            if (player.actions["Interact"].WasPressedThisFrame()) {
                RunManager.StartNewRun();
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

        DebugText.Draw(text, 24, Color.white, TextAnchor.MiddleCenter);
    }
}
