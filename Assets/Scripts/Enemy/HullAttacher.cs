using System;
using System.Collections.Generic;
using UnityEngine;

// Latches this body onto the ship's hull and rides along. Generic actuator: a behavior decides *when* (TryAttach).
// While attached: EnemyMover disabled, Rigidbody2D Kinematic, colliders are triggers (can't shove the ship or snag walls,
// weapons/modules can still find it), position follows a ship-relative anchor every FixedUpdate. Never reparented.
// Spot = where it touched; if taken, it slides to the nearest open spot around the hull, or (stackOutward) piles on top.
// Comes off on death, Detach(), or (optional) when the ship flies fast enough for long enough.
[RequireComponent(typeof(Rigidbody2D))]
public class HullAttacher : MonoBehaviour {
    [SerializeField] private int maxAttachedPerShip = 4;  // 0 = unlimited; counts every attached HullAttacher on that ship
    [SerializeField] private bool stackOutward = false;   // spot taken -> stack on top instead of sliding to an open spot
    [SerializeField] private int maxSearchSteps = 24;     // open-spot search around the hull (each step ~1 body width)
    [SerializeField] private int maxStackHeight = 6;
    [SerializeField] private LayerMask occupancyMask = 1 << 13; // Enemy

    [Header("Shake-off (0 = never)")]
    [SerializeField] private float shakeOffSpeed = 0f;    // ship speed that starts shaking it off
    [SerializeField] private float shakeOffTime = 1f;     // seconds above that speed before it lets go
    [SerializeField] private float detachSpeed = 4f;      // outward push when it lets go

    public event Action OnAttached;
    public event Action OnDetached;

    public bool IsAttached { get; private set; }
    public Rigidbody2D AttachedShip { get; private set; }
    public Vector2 LocalAnchor { get; private set; } // in the ship's local space (position + rotation)
    public bool StackOutward => stackOutward;
    public float BodyRadius => ComputeRadius();

    private Rigidbody2D rb;
    private EnemyMover mover;
    private Collider2D[] colliders;
    private bool[] wasTrigger;
    private RigidbodyType2D prevBodyType;
    private float shakeTimer;
    private readonly List<Collider2D> overlap = new List<Collider2D>();

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        mover = GetComponent<EnemyMover>();
        colliders = GetComponents<Collider2D>();
        wasTrigger = new bool[colliders.Length];
    }

    void OnValidate() {
        maxAttachedPerShip = Mathf.Max(0, maxAttachedPerShip);
        maxSearchSteps = Mathf.Max(0, maxSearchSteps);
        maxStackHeight = Mathf.Max(1, maxStackHeight);
        shakeOffSpeed = Mathf.Max(0f, shakeOffSpeed);
        shakeOffTime = Mathf.Max(0f, shakeOffTime);
        detachSpeed = Mathf.Max(0f, detachSpeed);
    }

    // Try to latch on at the hull point nearest to us. False = limit reached or no room
    public bool TryAttach(Rigidbody2D ship) {
        if (IsAttached || ship == null) return false;
        if (maxAttachedPerShip > 0 && CountAttached(ship) >= maxAttachedPerShip) return false;
        if (!FindSpot(ship, out Vector2 spot)) return false;

        // Switch to a passenger body
        prevBodyType = rb.bodyType;
        if (mover != null) mover.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        for (int i = 0; i < colliders.Length; i++) {
            wasTrigger[i] = colliders[i].isTrigger;
            colliders[i].isTrigger = true;
        }

        AttachedShip = ship;
        LocalAnchor = ShipToLocal(ship, spot);
        IsAttached = true;
        shakeTimer = 0f;
        rb.position = spot;
        transform.position = spot;

        OnAttached?.Invoke();
        return true;
    }

    public void Detach() {
        if (!IsAttached) return;
        var ship = AttachedShip;
        IsAttached = false;
        AttachedShip = null;

        for (int i = 0; i < colliders.Length; i++) colliders[i].isTrigger = wasTrigger[i];
        rb.bodyType = prevBodyType;
        if (mover != null) mover.enabled = true;

        // Pop off outward
        if (ship != null && rb.bodyType == RigidbodyType2D.Dynamic) {
            Vector2 outward = rb.position - ship.position;
            if (outward.sqrMagnitude > 0.0001f) rb.linearVelocity = outward.normalized * detachSpeed;
        }

        OnDetached?.Invoke();
    }

    void FixedUpdate() {
        if (!IsAttached) return;
        if (AttachedShip == null) {
            Detach();
            return;
        }

        // Scripts run before the physics step moves the ship, so aim for where it will be after this step (no lag)
        var ship = AttachedShip;
        float dt = Time.fixedDeltaTime;
        Vector2 shipPos = ship.position + (ship.simulated ? ship.linearVelocity * dt : Vector2.zero);
        float shipRot = ship.rotation + (ship.simulated ? ship.angularVelocity * dt : 0f);
        rb.MovePosition(shipPos + (Vector2)(Quaternion.Euler(0f, 0f, shipRot) * LocalAnchor));

        if (shakeOffSpeed <= 0f || !LevelGate.IsPlaying) return;
        if (AttachedShip.linearVelocity.magnitude >= shakeOffSpeed) shakeTimer += Time.fixedDeltaTime;
        else shakeTimer = 0f;
        if (shakeTimer >= shakeOffTime) Detach();
    }

    // Counted straight from the attached instances (no registry). Only runs when something tries to attach
    private static int CountAttached(Rigidbody2D ship) {
        int n = 0;
        foreach (var a in FindObjectsByType<HullAttacher>()) {
            if (a.IsAttached && a.AttachedShip == ship) n++;
        }
        return n;
    }

    private bool FindSpot(Rigidbody2D ship, out Vector2 spot) {
        float r = ComputeRadius();
        Vector2 center = ship.position;
        Vector2 dir = rb.position - center;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();

        // Where we touched: hull point toward us, sitting on the surface
        spot = SurfaceSpot(ship, dir, r);
        var occupant = Occupant(ship, spot, r);
        if (occupant == null) return true;

        if (stackOutward) {
            // Pile on top of whoever is there, outward along the hull normal
            for (int i = 0; i < maxStackHeight && occupant != null; i++) {
                spot = (Vector2)occupant.transform.position + dir * (occupant.BodyRadius + r);
                occupant = Occupant(ship, spot, r);
            }
            return occupant == null;
        }

        // Slide around the hull to the nearest open spot (alternating sides)
        float hullDist = Mathf.Max(0.5f, Vector2.Distance(center, spot) - r);
        float step = (2f * r * 1.05f) / hullDist * Mathf.Rad2Deg;
        for (int i = 1; i <= maxSearchSteps; i++) {
            float angle = step * ((i + 1) / 2) * (i % 2 == 0 ? -1f : 1f);
            if (Mathf.Abs(angle) > 180f) break;
            Vector2 d = Quaternion.Euler(0f, 0f, angle) * dir;
            Vector2 candidate = SurfaceSpot(ship, d, r);
            if (Occupant(ship, candidate, r) == null) {
                spot = candidate;
                return true;
            }
        }
        return false;
    }

    // Point just outside the hull in direction dir (from the ship's center)
    private static Vector2 SurfaceSpot(Rigidbody2D ship, Vector2 dir, float radius) {
        Vector2 hullPoint = ship.ClosestPoint(ship.position + dir * 1000f);
        return hullPoint + dir * radius;
    }

    // Another attached body of this ship overlapping that spot (triggers included)
    private HullAttacher Occupant(Rigidbody2D ship, Vector2 spot, float radius) {
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(occupancyMask);
        Physics2D.OverlapCircle(spot, radius * 0.95f, filter, overlap);
        foreach (var col in overlap) {
            var body = col.attachedRigidbody;
            if (body == null || body == rb) continue;
            if (body.TryGetComponent(out HullAttacher other) && other.IsAttached && other.AttachedShip == ship) return other;
        }
        return null;
    }

    private static Vector2 ShipToLocal(Rigidbody2D ship, Vector2 world) {
        return Quaternion.Euler(0f, 0f, -ship.rotation) * (world - ship.position);
    }

    private static Vector2 LocalToShip(Rigidbody2D ship, Vector2 local) {
        return ship.position + (Vector2)(Quaternion.Euler(0f, 0f, ship.rotation) * local);
    }

    private float ComputeRadius() {
        if (TryGetComponent(out CircleCollider2D circle)) {
            Vector3 s = transform.lossyScale;
            return circle.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
        }
        var col = GetComponent<Collider2D>();
        return col != null ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.y) : 0.5f;
    }

    void OnDrawGizmosSelected() {
        if (!IsAttached || AttachedShip == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(AttachedShip.position, LocalToShip(AttachedShip, LocalAnchor));
    }
}
