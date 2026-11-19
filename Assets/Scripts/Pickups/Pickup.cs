using UnityEngine;

// A collectible drop (e.g. Light). Drifts to a stop after being launched, gets pulled toward the ship when close,
// and is collected on touch through the ship's IResourceReceiver (stays if nothing is accepted).
// Despawns after its lifetime, blinking near the end. Everything freezes while the level isn't Playing.
// Prefab: trigger collider + Kinematic Rigidbody2D (gravity 0) on the Pickup layer (16, collides with Spaceship only).
[RequireComponent(typeof(Rigidbody2D))]
public class Pickup : MonoBehaviour {
    [SerializeField] private ResourceType resourceType = ResourceType.Light;
    [SerializeField] private float amount = 1f;

    [Header("Movement")]
    [SerializeField] private float driftDeceleration = 6f;   // launch speed bleeds off at this rate
    [SerializeField] private float magnetRadius = 5f;        // measured to the ship hull
    [SerializeField] private float magnetSpeed = 16f;        // faster than the ship (~10) so it catches up
    [SerializeField] private float magnetAcceleration = 40f;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 15f;           // 0 = forever
    [SerializeField] private float blinkTime = 3f;           // blink during the last seconds
    [SerializeField] private SpriteRenderer spriteRenderer;  // auto-found in children if empty

    public ResourceType ResourceType => resourceType;
    public float Amount => amount;
    public Vector2 Velocity => velocity;

    private Rigidbody2D rb;
    private Vector2 velocity;
    private float age;
    private bool warnedNoReceiver;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Never solid: a solid drop touching the hull could crash the ship
        foreach (var col in GetComponents<Collider2D>()) col.isTrigger = true;
    }

    void OnValidate() {
        amount = Mathf.Max(0f, amount);
        driftDeceleration = Mathf.Max(0f, driftDeceleration);
        magnetRadius = Mathf.Max(0f, magnetRadius);
        magnetSpeed = Mathf.Max(0f, magnetSpeed);
        magnetAcceleration = Mathf.Max(0f, magnetAcceleration);
        lifetime = Mathf.Max(0f, lifetime);
        blinkTime = Mathf.Max(0f, blinkTime);
    }

    // Initial push, e.g. the scatter from DropOnDeath
    public void Launch(Vector2 initialVelocity) {
        velocity = initialVelocity;
    }

    void FixedUpdate() {
        if (!LevelGate.IsPlaying) return; // frozen: no movement, no aging

        age += Time.fixedDeltaTime;
        if (lifetime > 0f && age >= lifetime) {
            Destroy(gameObject);
            return;
        }

        Vector2 pos = rb.position;
        Vector2 desired = Vector2.zero;
        float accel = driftDeceleration;

        var ship = LevelGate.Ship;
        if (ship != null && Vector2.Distance(pos, ship.ClosestPoint(pos)) <= magnetRadius) {
            Vector2 toShip = ship.position - pos;
            if (toShip.sqrMagnitude > 0.0001f) desired = toShip.normalized * magnetSpeed;
            accel = magnetAcceleration;
        }

        velocity = Vector2.MoveTowards(velocity, desired, accel * Time.fixedDeltaTime);
        if (velocity != Vector2.zero) rb.MovePosition(pos + velocity * Time.fixedDeltaTime);
    }

    void Update() {
        if (spriteRenderer == null || lifetime <= 0f) return;
        float remaining = lifetime - age;
        spriteRenderer.enabled = remaining > blinkTime || Mathf.Repeat(remaining * 8f, 1f) > 0.35f;
    }

    void OnTriggerEnter2D(Collider2D other) {
        TryCollect(other);
    }

    // Retries while touching (e.g. the receiver was full and has room again)
    void OnTriggerStay2D(Collider2D other) {
        TryCollect(other);
    }

    private void TryCollect(Collider2D other) {
        var ship = LevelGate.Ship;
        if (ship == null || other.attachedRigidbody != ship || amount <= 0f) return;

        if (!ship.TryGetComponent(out IResourceReceiver receiver)) {
            if (!warnedNoReceiver) Debug.LogWarning($"[Pickup] {name}: ship has no IResourceReceiver, can't collect", this);
            warnedNoReceiver = true;
            return;
        }

        float accepted = receiver.Receive(resourceType, amount);
        if (accepted <= 0f) return;

        amount -= accepted; // partial accept leaves the rest in the pickup
        if (amount > 0.0001f) return;
        amount = 0f;
        Destroy(gameObject);
    }
}
