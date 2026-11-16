using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerJoinManager : MonoBehaviour {
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private int maxPlayers;

    [Header("Roster")]
    [Tooltip("Respawn players already in the PlayerRoster when the scene starts")]
    [SerializeField] private bool spawnFromRosterOnStart = true;
    [Tooltip("Allow new players to join with their join key")]
    [SerializeField] private bool allowLiveJoining = true;

    public Action<GameObject> OnPlayerJoined;

    // Must happen in Start: PlayerAndSpaceshipSync subscribes to OnPlayerJoined in Awake
    private void Start() {
        if (!spawnFromRosterOnStart) return;

        foreach (var entry in PlayerRoster.Entries) {
            if (entry.device != null) SpawnPlayer(entry.controlScheme, entry.device);
        }
    }

    private void Update() {
        if (!allowLiveJoining || PlayerRoster.Count >= maxPlayers) return;

        foreach (var gamepad in Gamepad.all)
            if (gamepad.startButton.wasPressedThisFrame && !IsDeviceAlreadyPaired(gamepad))
                JoinPlayer("Gamepad", gamepad);

        var kb = Keyboard.current;

        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame && !IsKeyboardSchemeActive("KeyboardP1"))
            JoinPlayer("KeyboardP1", kb);

        if (kb.enterKey.wasPressedThisFrame && !IsKeyboardSchemeActive("KeyboardP2"))
            JoinPlayer("KeyboardP2", kb);

        if (kb.rightShiftKey.wasPressedThisFrame && !IsKeyboardSchemeActive("KeyboardP3"))
            JoinPlayer("KeyboardP3", kb);

        if (kb.numpadEnterKey.wasPressedThisFrame && !IsKeyboardSchemeActive("KeyboardP4"))
            JoinPlayer("KeyboardP4", kb);
    }

    private void JoinPlayer(string schemeName, InputDevice device) {
        PlayerRoster.Add(schemeName, device);
        SpawnPlayer(schemeName, device);
    }

    private void SpawnPlayer(string schemeName, InputDevice device) {
        var pi = PlayerInput.Instantiate(playerPrefab, controlScheme: schemeName, pairWithDevice: device);
        pi.actions.Enable();

        Debug.Log($"Player joined using {schemeName}!");
        OnPlayerJoined?.Invoke(pi.gameObject);
    }

    private bool IsDeviceAlreadyPaired(InputDevice device) {
        return PlayerRoster.UsesDevice(device);
    }

    private bool IsKeyboardSchemeActive(string schemeName) {
        return PlayerRoster.UsesScheme(schemeName);
    }
}
