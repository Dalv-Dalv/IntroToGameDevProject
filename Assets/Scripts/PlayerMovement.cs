using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour {
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private float moveSpeed;

    private InputAction moveAction;
    
    private void Start() {
        moveAction = playerInput.actions["Move"];
    }

    void Update() {
        if (!playerInput.actions) return;
        var move = moveAction.ReadValue<Vector2>();
        
        transform.position += new Vector3(move.x, move.y).normalized * (Time.deltaTime * moveSpeed);
    }
}
