using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput), typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour { 
    [Header("References")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Rigidbody2D rb;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private DynamicsParams dynamics = new DynamicsParams(2f, 0.5f, 2f);
    
    
    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private LayerMask groundMask;
    
    [Header("Coyote Time Settings")]
    [SerializeField] private float hangTime;
    [SerializeField] private float jumpBufferDuration;
    
    private float hangCounter;
    private float jumpBufferCounter;
    private bool isGrounded;
    private bool hasJumped;

    private InputAction moveAction;
    private InputAction jumpAction;
    private Vector2 moveInput;
    
    private V2SecondOrderDynamics velocityDynamics;
    private Vector2 previousTargetVelocity;
    
    private void Start() {
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        
        jumpAction.performed += _ => Jump();
        
        velocityDynamics = new V2SecondOrderDynamics(dynamics.f, dynamics.z, dynamics.r, Vector2.zero);
    }

    private void Jump() {
        jumpBufferCounter = jumpBufferDuration;
        Debug.Log("Jumped");
    }

    private void Update() {
        moveInput = moveAction.ReadValue<Vector2>();
    }

    private void FixedUpdate() {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);

        if (isGrounded) {
            hangCounter = hangTime;
        } else {
            hasJumped = false;
            hangCounter = Mathf.Clamp(hangCounter - Time.fixedDeltaTime, -1, hangTime);
        }
        
        jumpBufferCounter = Mathf.Clamp(jumpBufferCounter - Time.fixedDeltaTime, -1, jumpBufferDuration);

        if (!hasJumped && jumpBufferCounter >= 0 && hangCounter > 0) {
            rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);
            hasJumped = true;
        }

        velocityDynamics.ChangeParameters(Mathf.Max(0.01f, dynamics.f), dynamics.z, dynamics.r);

        var targetVelocity = new Vector2(moveInput.x * moveSpeed, 0f);
        var targetVelocityDerivative = (targetVelocity - previousTargetVelocity) / Time.fixedDeltaTime;
        previousTargetVelocity = targetVelocity;
        
        var smoothedVelocity = velocityDynamics.Update(Time.fixedDeltaTime, targetVelocity, targetVelocityDerivative);

        var velocityDifferenceX = smoothedVelocity.x - rb.linearVelocityX;
        var forceNeededX = (velocityDifferenceX / Time.fixedDeltaTime) * rb.mass;

        rb.AddForce(new Vector2(forceNeededX, 0f), ForceMode2D.Force);
    }

#if UNITY_EDITOR
    private void OnValidate() {
        if (!Application.isPlaying) return;
        velocityDynamics.ChangeParameters(Mathf.Max(0.01f, dynamics.f), dynamics.z, dynamics.r);
    }
#endif

    private void OnDrawGizmos() {
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}