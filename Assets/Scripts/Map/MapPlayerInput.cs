using UnityEngine;
using UnityEngine.InputSystem;

// On the map player prefab, any player can move the cursor and confirm
[RequireComponent(typeof(PlayerInput))]
public class MapPlayerInput : MonoBehaviour {
    private InputAction moveAction;
    private InputAction interactAction;
    private MapCursor cursor;
    private bool moveHeld;

    void Start() {
        var playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        interactAction = playerInput.actions["Interact"];
        cursor = FindAnyObjectByType<MapCursor>();
    }

    void Update() {
        // Only move once per push of the stick or key
        float x = moveAction.ReadValue<Vector2>().x;
        bool held = Mathf.Abs(x) > 0.5f;
        if (held && !moveHeld) cursor.Move(x > 0f ? 1 : -1);
        moveHeld = held;

        if (interactAction.WasPressedThisFrame()) cursor.Confirm();
    }
}
