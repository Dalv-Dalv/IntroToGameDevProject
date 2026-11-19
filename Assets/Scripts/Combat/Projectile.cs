using System;
using UnityEngine;

// A straight-flying shot (Kinematic trigger). Damages the first thing it touches on hitLayers, despawns on solid
// colliders on obstacleLayers (walls) and after its lifetime. Never hits its owner. Frozen while the level isn't Playing.
// Prefab: trigger collider + Kinematic Rigidbody2D (gravity 0) on a projectile layer (EnemyProjectile 14 / ShipProjectile 15),
// so it can never crash the ship (only Default is a crash layer).
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour {
    [SerializeField] private float speed = 12f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private DamageType damageType = DamageType.Projectile;
    [SerializeField] private LayerMask hitLayers = 1 << 8;                   // Spaceship (hull)
    [SerializeField] private LayerMask obstacleLayers = (1 << 0) | (1 << 6); // Default (obstacles) + Ground
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private bool rotateToDirection = true;

    public event Action<Projectile, Collider2D> OnImpact; // hit or blocked (not on lifetime expiry)

    public GameObject Owner { get; private set; }
    public Vector2 Velocity => velocity;
    public float Damage => damage;

    private Rigidbody2D rb;
    private Vector2 velocity;
    private float age;
    private bool spent;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        // Never solid: a solid shot would shove (or crash) whatever it touches
        foreach (var col in GetComponents<Collider2D>()) col.isTrigger = true;
    }

    void OnValidate() {
        speed = Mathf.Max(0f, speed);
        damage = Mathf.Max(0f, damage);
        lifetime = Mathf.Max(0.01f, lifetime);
    }

    // Call right after Instantiate. owner = the shooter's root (never hit)
    public void Launch(Vector2 direction, GameObject owner = null) {
        Owner = owner;
        velocity = direction.sqrMagnitude > 0.0001f ? direction.normalized * speed : Vector2.zero;
        if (rotateToDirection && velocity != Vector2.zero) {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            rb.rotation = angle;
        }
    }

    void FixedUpdate() {
        if (!LevelGate.IsPlaying) return; // frozen

        age += Time.fixedDeltaTime;
        if (age >= lifetime) {
            Destroy(gameObject);
            return;
        }
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (spent || !LevelGate.IsPlaying || IsOwner(other)) return;

        int bit = 1 << other.gameObject.layer;
        if ((hitLayers.value & bit) != 0) {
            if (DamageUtil.TryGetDamageable(other, out var target)) {
                target.DealDamage(new DamageInfo(damage, damageType, rb.position, Owner));
            }
            Impact(other);
            return;
        }

        // Walls: solid colliders only (skip e.g. console triggers on Default)
        if ((obstacleLayers.value & bit) != 0 && !other.isTrigger) Impact(other);
    }

    private bool IsOwner(Collider2D other) {
        if (Owner == null) return false;
        var body = other.attachedRigidbody;
        if (body != null && body.gameObject == Owner) return true;
        return other.transform.IsChildOf(Owner.transform);
    }

    private void Impact(Collider2D other) {
        spent = true;
        OnImpact?.Invoke(this, other);
        Destroy(gameObject);
    }
}
