using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput), typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour {
    [Header("References")] [SerializeField]
    PlayerInput playerInput;

    [SerializeField] Rigidbody2D rb;

    [Header("Movement Settings")] [SerializeField]
    float moveSpeed;

    [SerializeField] float jumpForce;
    [SerializeField] DynamicsParams dynamics = new DynamicsParams(2f);

    [Header("Ground Check Settings")] [SerializeField]
    Transform groundCheck;

    [SerializeField] float groundCheckRadius;
    [SerializeField] LayerMask groundMask;

    [Header("Coyote Time Settings")] [SerializeField]
    float hangTime;

    [SerializeField] float jumpBufferDuration;

    float hangCounter;
    bool hasJumped;
    bool isGrounded;
    bool isUsingModule;
    InputAction jumpAction;
    float jumpBufferCounter;

    InputAction moveAction;
    Vector2 moveInput;
    Vector2 previousTargetVelocity;

    V2SecondOrderDynamics velocityDynamics;

    void Start() {
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];

        jumpAction.performed += _ => Jump();

        velocityDynamics = new V2SecondOrderDynamics(dynamics.f, dynamics.z, dynamics.r, Vector2.zero);
    }

    void Update() {
        moveInput = isUsingModule ? Vector2.zero : moveAction.ReadValue<Vector2>();
    }

    void FixedUpdate() {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);

        if (isGrounded) {
            hangCounter = hangTime;
        } else {
            hasJumped = false;
            hangCounter = Mathf.Clamp(hangCounter - Time.fixedDeltaTime, -1f, hangTime);
        }

        jumpBufferCounter = Mathf.Clamp(jumpBufferCounter - Time.fixedDeltaTime, -1f, jumpBufferDuration);

        if (!isUsingModule && !hasJumped && jumpBufferCounter >= 0f && hangCounter > 0f) {
            rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);
            hasJumped = true;
        }

        velocityDynamics.ChangeParameters(Mathf.Max(0.01f, dynamics.f), dynamics.z, dynamics.r);

        Vector2 targetVelocity = new Vector2(moveInput.x * moveSpeed, 0f);
        Vector2 targetVelocityDerivative = (targetVelocity - previousTargetVelocity) / Time.fixedDeltaTime;
        previousTargetVelocity = targetVelocity;

        Vector2 smoothedVelocity = velocityDynamics.Update(Time.fixedDeltaTime, targetVelocity, targetVelocityDerivative);

        float velocityDifferenceX = smoothedVelocity.x - rb.linearVelocityX;
        float forceNeededX = velocityDifferenceX / Time.fixedDeltaTime * rb.mass;

        rb.AddForce(new Vector2(forceNeededX, 0f), ForceMode2D.Force);
    }

    void OnDrawGizmos() {
        if (groundCheck != null) {
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

#if UNITY_EDITOR
    void OnValidate() {
        if (!Application.isPlaying || velocityDynamics == null) return;
        velocityDynamics.ChangeParameters(Mathf.Max(0.01f, dynamics.f), dynamics.z, dynamics.r);
    }
#endif

    public void SetUsingModule(bool usingModule) {
        isUsingModule = usingModule;
        if (isUsingModule) {
            moveInput = Vector2.zero;
            jumpBufferCounter = -1f;
        }
    }

    void Jump() {
        if (isUsingModule) return;
        jumpBufferCounter = jumpBufferDuration;
        Debug.Log("Jumped");
    }
    public void SetPhysicsRigidbody(Rigidbody2D proxyRb) {
        rb = proxyRb;
        if (groundCheck != null) {
            Vector3 localOffset = groundCheck.localPosition;
            groundCheck.SetParent(proxyRb.transform, false);
            groundCheck.localPosition = localOffset;
        }
    }
}
