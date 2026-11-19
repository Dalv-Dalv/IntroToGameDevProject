using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerJoinManager : MonoBehaviour {
    [SerializeField] GameObject playerPrefab;
    [SerializeField] int maxPlayers;

    public Action<GameObject> OnPlayerJoined;
    int playersJoined;
    void Update() {
        //Debug.Log(playersJoined);
        if (playersJoined >= maxPlayers) return;

        foreach (Gamepad gamepad in Gamepad.all)
            if (gamepad.startButton.wasPressedThisFrame && !IsDeviceAlreadyPaired(gamepad))
                JoinPlayer("Gamepad", gamepad);

        Keyboard kb = Keyboard.current;

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

    void JoinPlayer(string schemeName, InputDevice device) {
        PlayerInput pi = PlayerInput.Instantiate(playerPrefab, controlScheme: schemeName, pairWithDevice: device);
        pi.actions.Enable();

        playersJoined++;

        Debug.Log($"Player {playersJoined} joined using {schemeName}!");
        OnPlayerJoined?.Invoke(pi.gameObject);
    }

    bool IsDeviceAlreadyPaired(InputDevice device) {
        return PlayerInput.all.Any(p => p.devices.Contains(device));
    }

    bool IsKeyboardSchemeActive(string schemeName) {
        return PlayerInput.all.Any(p => p.currentControlScheme == schemeName);
    }
}
