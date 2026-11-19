using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput), typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour {
    [Header("References")]
    [SerializeField] PlayerInput playerInput;
    [SerializeField] Rigidbody2D rb;

    [Header("Movement Settings")]
    [SerializeField] float moveSpeed;
    [SerializeField] float jumpForce;
    [SerializeField] DynamicsParams dynamics = new DynamicsParams(2f);

    [Header("Ground Check Settings")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius;
    [SerializeField] LayerMask groundMask;
    [SerializeField] LayerMask oneWayPlatformMask;
    [SerializeField] float dropThroughDuration = 0.25f;

    [Header("Ladder Settings")]
    [SerializeField] LayerMask ladderMask;
    [SerializeField] float climbSpeed = 5f;
    [SerializeField] float ladderHorizontalMultiplier = 0.6f;
    [SerializeField] float ladderSnapSpeed = 14f;

    [Header("Coyote Time Settings")]
    [SerializeField] float hangTime;
    [SerializeField] float jumpBufferDuration;

    readonly List<Collider2D> attachedColliders = new List<Collider2D>();
    readonly List<Collider2D> overlapResults = new List<Collider2D>();
    readonly HashSet<Collider2D> ignoredPlatforms = new HashSet<Collider2D>();
    readonly List<Collider2D> platformsToRestore = new List<Collider2D>();

    float hangCounter;
    bool hasJumped;
    bool isGrounded;
    bool isUsingModule;
    bool isClimbing;
    bool isSnappingToLadder;
    float ladderCenterX;
    float dropThroughTimer;
    float defaultGravityScale;

    InputAction jumpAction;
    InputAction moveAction;
    float jumpBufferCounter;
    Vector2 moveInput;
    Vector2 previousTargetVelocity;

    V2SecondOrderDynamics velocityDynamics;

    void Start() {
        defaultGravityScale = rb.gravityScale;

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        jumpAction.performed += _ => Jump();

        velocityDynamics = new V2SecondOrderDynamics(dynamics.f, dynamics.z, dynamics.r, Vector2.zero);
    }

    void Update() {
        moveInput = isUsingModule ? Vector2.zero : moveAction.ReadValue<Vector2>();
    }

    void FixedUpdate() {
        dropThroughTimer = Mathf.Max(0f, dropThroughTimer - Time.fixedDeltaTime);
        jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - Time.fixedDeltaTime);

        EvaluateGround(out bool onSolidGround, out bool onOneWayPlatform);
        isGrounded = (onSolidGround || onOneWayPlatform) && rb.linearVelocityY <= 0.1f;

        Collider2D ladderCollider = GetLadderCollider();
        bool touchingLadder = ladderCollider;

        if (!isUsingModule && !isClimbing && jumpBufferCounter > 0f && !onSolidGround && onOneWayPlatform && moveInput.y < -0.3f)
            StartDropThrough();
        

        if (!isUsingModule && !isClimbing && touchingLadder && dropThroughTimer <= 0f) {
            bool wantsClimbUp = moveInput.y > 0.3f;
            bool wantsClimbDown = moveInput.y < -0.3f && !onSolidGround;

            if (wantsClimbUp || wantsClimbDown) StartClimbing(ladderCollider);
        }

        if (isClimbing) {
            if (isUsingModule || !touchingLadder || (onSolidGround && moveInput.y < -0.1f)) {
                StopClimbing();
            } else if (jumpBufferCounter > 0f) {
                jumpBufferCounter = 0f;
                if (moveInput.y < -0.3f) {
                    StopClimbing();
                    StartDropThrough();
                } else {
                    StopClimbing();
                    ExecuteJump();
                }
            } else {
                rb.linearVelocity = new Vector2(rb.linearVelocityX, moveInput.y * climbSpeed);
            }
        }

        if (!isClimbing) {
            if (isGrounded) {
                hangCounter = hangTime;
            } else {
                hasJumped = false;
                hangCounter = Mathf.Clamp(hangCounter - Time.fixedDeltaTime, -1f, hangTime);
            }

            if (!isUsingModule && !hasJumped && jumpBufferCounter > 0f && hangCounter > 0f) {
                ExecuteJump();
            }
        }

        UpdateIgnoredPlatforms();

        velocityDynamics.ChangeParameters(Mathf.Max(0.01f, dynamics.f), dynamics.z, dynamics.r);

        Vector2 targetVelocity = CalculateHorizontalTargetVelocity();
        Vector2 targetVelocityDerivative = (targetVelocity - previousTargetVelocity) / Time.fixedDeltaTime;
        previousTargetVelocity = targetVelocity;

        Vector2 smoothedVelocity = velocityDynamics.Update(Time.fixedDeltaTime, targetVelocity, targetVelocityDerivative);

        float velocityDifferenceX = smoothedVelocity.x - rb.linearVelocityX;
        float forceNeededX = velocityDifferenceX / Time.fixedDeltaTime * rb.mass;

        rb.AddForce(new Vector2(forceNeededX, 0f), ForceMode2D.Force);
    }

    void EvaluateGround(out bool onSolidGround, out bool onOneWayPlatform) {
        onSolidGround = false;
        onOneWayPlatform = false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundMask | oneWayPlatformMask);
        filter.useTriggers = false;

        overlapResults.Clear();
        Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, filter, overlapResults);

        foreach (Collider2D hit in overlapResults) {
            if (!hit || IsOwnCollider(hit) || ignoredPlatforms.Contains(hit)) continue;

            if (IsOneWayPlatform(hit)) {
                onOneWayPlatform = true;
            } else {
                onSolidGround = true;
            }
        }
    }

    void StartDropThrough() {
        dropThroughTimer = dropThroughDuration;
        jumpBufferCounter = 0f;
        hangCounter = -1f;

        IgnoreNearbyOneWayPlatforms();

        if (rb.linearVelocityY >= 0f) {
            rb.linearVelocity = new Vector2(rb.linearVelocityX, -1.5f);
        }
    }

    void StartClimbing(Collider2D ladderCollider) {
        isClimbing = true;
        hasJumped = false;
        isSnappingToLadder = true;
        ladderCenterX = ladderCollider.bounds.center.x;

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(rb.linearVelocityX, 0f);

        IgnoreNearbyOneWayPlatforms();
    }

    void StopClimbing() {
        isClimbing = false;
        isSnappingToLadder = false;
        rb.gravityScale = defaultGravityScale;
    }

    void ExecuteJump() {
        rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);
        hasJumped = true;
        jumpBufferCounter = 0f;
        hangCounter = -1f;
    }

    Vector2 CalculateHorizontalTargetVelocity() {
        if (!isClimbing) return new Vector2(moveInput.x * moveSpeed, 0f);
        
        if (Mathf.Abs(moveInput.x) > 0.05f)
            isSnappingToLadder = false;
        
        if (!isSnappingToLadder)
            return new Vector2(moveInput.x * moveSpeed * ladderHorizontalMultiplier, 0f);
        
        float offsetX = ladderCenterX - rb.position.x;
        if (Mathf.Abs(offsetX) <= 0.01f) {
            isSnappingToLadder = false;
            return Vector2.zero;
        }

        float snapVelX = Mathf.Clamp(offsetX * ladderSnapSpeed, -moveSpeed, moveSpeed);
        return new Vector2(snapVelX, 0f);
    }

    void IgnoreNearbyOneWayPlatforms() {
        if (!TryGetBodyBounds(out Bounds bodyBounds)) return;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundMask | oneWayPlatformMask);
        filter.useTriggers = false;

        Vector2 checkCenter = bodyBounds.center;
        Vector2 checkSize = new Vector2(bodyBounds.size.x + 0.1f, bodyBounds.size.y + 0.5f);

        overlapResults.Clear();
        Physics2D.OverlapBox(checkCenter, checkSize, 0f, filter, overlapResults);

        foreach (Collider2D hit in overlapResults) {
            if (!hit || IsOwnCollider(hit) || !IsOneWayPlatform(hit)) continue;
            SetIgnorePlatform(hit, true);
        }
    }

    void UpdateIgnoredPlatforms() {
        if (isClimbing || dropThroughTimer > 0f) {
            IgnoreNearbyOneWayPlatforms();
            return;
        }

        if (ignoredPlatforms.Count == 0) return;

        rb.GetAttachedColliders(attachedColliders);
        platformsToRestore.Clear();

        foreach (Collider2D platform in ignoredPlatforms) {
            if (!platform) {
                platformsToRestore.Add(platform);
                continue;
            }

            bool stillOverlapping = false;
            foreach (Collider2D playerCol in attachedColliders) {
                if (!playerCol || !playerCol.enabled || playerCol.isTrigger) continue;

                ColliderDistance2D dist = Physics2D.Distance(playerCol, platform);
                
                if (dist.isValid && dist.isOverlapped) {
                    stillOverlapping = true;
                    break;
                }
            }

            if (!stillOverlapping) {
                platformsToRestore.Add(platform);
            }
        }

        foreach (Collider2D platform in platformsToRestore) {
            SetIgnorePlatform(platform, false);
        }
    }

    void SetIgnorePlatform(Collider2D platform, bool ignore) {
        if (!platform || !rb) return;

        rb.GetAttachedColliders(attachedColliders);
        foreach (Collider2D playerCol in attachedColliders) {
            if (!playerCol) continue;
            Physics2D.IgnoreCollision(playerCol, platform, ignore);
        }

        if (ignore) {
            ignoredPlatforms.Add(platform);
        } else {
            ignoredPlatforms.Remove(platform);
        }
    }

    void RestoreAllIgnoredPlatforms() {
        if (ignoredPlatforms.Count == 0) return;

        platformsToRestore.Clear();
        platformsToRestore.AddRange(ignoredPlatforms);
        
        foreach (Collider2D col in platformsToRestore) {
            SetIgnorePlatform(col, false);
        }
    }

    bool IsOneWayPlatform(Collider2D col) {
        if (!col) return false;
        if ((1 << col.gameObject.layer & oneWayPlatformMask) != 0) return true;
        
        return col.usedByEffector && col.GetComponent<PlatformEffector2D>();
    }

    bool IsOwnCollider(Collider2D col) {
        return col.attachedRigidbody == rb;
    }

    Collider2D GetLadderCollider() {
        if (!TryGetBodyBounds(out Bounds bodyBounds)) return null;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(ladderMask);
        filter.useTriggers = true;

        Vector2 checkCenter = new Vector2(bodyBounds.center.x, bodyBounds.center.y - 0.1f);
        Vector2 checkSize = new Vector2(bodyBounds.size.x * 0.8f, bodyBounds.size.y + 0.2f);

        overlapResults.Clear();
        Physics2D.OverlapBox(checkCenter, checkSize, 0f, filter, overlapResults);

        return overlapResults.Count > 0 ? overlapResults[0] : null;
    }

    bool TryGetBodyBounds(out Bounds bounds) {
        bounds = default;
        if (!rb) return false;

        rb.GetAttachedColliders(attachedColliders);
        bool found = false;

        foreach (Collider2D col in attachedColliders) {
            if (!col || !col.enabled || col.isTrigger) continue;

            if (!found) {
                bounds = col.bounds;
                found = true;
            } else {
                bounds.Encapsulate(col.bounds);
            }
        }

        return found;
    }

    void OnCollisionEnter2D(Collision2D collision) {
        if ((isClimbing || dropThroughTimer > 0f) && IsOneWayPlatform(collision.collider)) {
            SetIgnorePlatform(collision.collider, true);
        }
    }

    void OnCollisionStay2D(Collision2D collision) {
        if ((isClimbing || dropThroughTimer > 0f) && IsOneWayPlatform(collision.collider)) {
            SetIgnorePlatform(collision.collider, true);
        }
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
        
        if (!isUsingModule) return;
        
        moveInput = Vector2.zero;
        jumpBufferCounter = 0f;
        
        if (isClimbing) StopClimbing();
    }

    void Jump() {
        if (isUsingModule) return;
        jumpBufferCounter = Mathf.Max(0.05f, jumpBufferDuration);
    }

    public void SetPhysicsRigidbody(Rigidbody2D proxyRb) {
        RestoreAllIgnoredPlatforms();

        rb = proxyRb;
        defaultGravityScale = proxyRb.gravityScale;

        if (!groundCheck)  return;
        Vector3 localOffset = groundCheck.localPosition;
        groundCheck.SetParent(proxyRb.transform, false);
        groundCheck.localPosition = localOffset;
    }
}