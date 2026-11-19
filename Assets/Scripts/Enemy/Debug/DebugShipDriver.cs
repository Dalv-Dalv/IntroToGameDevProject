using UnityEngine;
using UnityEngine.InputSystem;

// Test only: fly the ship with WASD / arrows, since test scenes have no crew at the steering console
[RequireComponent(typeof(MovementModule))]
public class DebugShipDriver : MonoBehaviour {
    private MovementModule movement;

    void Awake() {
        movement = GetComponent<MovementModule>();
    }

    void Update() {
        var kb = Keyboard.current;
        if (kb == null) return;

        Vector2 input = Vector2.zero;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;

        movement.SetMoveInput(input);
    }
}
