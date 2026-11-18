using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerJoinManager : MonoBehaviour {
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private int maxPlayers;

    public Action<GameObject> OnPlayerJoined;

    // Players from earlier scenes come back from the roster
    private void Start() {
        PlayerRoster.SpawnAll(playerPrefab, HandleSpawned);
    }

    private void Update() {
        if (PlayerRoster.Count >= maxPlayers) return;

        foreach (var gamepad in Gamepad.all)
            if (gamepad.startButton.wasPressedThisFrame && !PlayerRoster.UsesDevice(gamepad))
                JoinPlayer("Gamepad", gamepad);

        var kb = Keyboard.current;

        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame && !PlayerRoster.UsesScheme("KeyboardP1"))
            JoinPlayer("KeyboardP1", kb);

        if (kb.enterKey.wasPressedThisFrame && !PlayerRoster.UsesScheme("KeyboardP2"))
            JoinPlayer("KeyboardP2", kb);

        if (kb.rightShiftKey.wasPressedThisFrame && !PlayerRoster.UsesScheme("KeyboardP3"))
            JoinPlayer("KeyboardP3", kb);

        if (kb.numpadEnterKey.wasPressedThisFrame && !PlayerRoster.UsesScheme("KeyboardP4"))
            JoinPlayer("KeyboardP4", kb);
    }

    private void JoinPlayer(string schemeName, InputDevice device) {
        PlayerRoster.Add(schemeName, device);
        HandleSpawned(PlayerRoster.Spawn(playerPrefab, schemeName, device));
    }

    private void HandleSpawned(PlayerInput player) {
        player.actions.Enable();
        Debug.Log($"Player joined using {player.currentControlScheme}!");
        OnPlayerJoined?.Invoke(player.gameObject);
    }
}
