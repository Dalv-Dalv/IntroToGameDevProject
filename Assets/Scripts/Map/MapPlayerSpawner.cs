using UnityEngine;
using UnityEngine.InputSystem;

// Creates one map player for every player in the roster
public class MapPlayerSpawner : MonoBehaviour {
    [SerializeField] private GameObject mapPlayerPrefab;

    void Start() {
        // Map scene played directly in the editor
        if (PlayerRoster.Count == 0) {
            PlayerInput.Instantiate(mapPlayerPrefab, controlScheme: "KeyboardP1", pairWithDevice: Keyboard.current);
        }

        foreach (var entry in PlayerRoster.Entries) {
            if (entry.device == null) continue;
            PlayerInput.Instantiate(mapPlayerPrefab, controlScheme: entry.controlScheme, pairWithDevice: entry.device);
        }
    }
}
