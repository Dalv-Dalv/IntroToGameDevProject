using UnityEngine;

// On a hit that has a sourcePos: push the enemy away and pause its EnemyMover so the push plays out.
// Skips non-Dynamic bodies (e.g. latched onto the hull). Flash is separate (DamageFlash).
[RequireComponent(typeof(Damageable), typeof(Rigidbody2D))]
public class EnemyKnockback : MonoBehaviour {
    [SerializeField] private float knockbackImpulse = 7f;
    [SerializeField] private float steeringPause = 0.3f; // mover stops steering for this long after a hit

    private Damageable damageable;
    private Rigidbody2D rb;
    private EnemyMover mover;

    void Awake() {
        damageable = GetComponent<Damageable>();
        rb = GetComponent<Rigidbody2D>();
        mover = GetComponent<EnemyMover>();
    }

    void OnEnable() {
        damageable.OnDamaged += HandleDamaged;
    }

    void OnDisable() {
        damageable.OnDamaged -= HandleDamaged;
    }

    void OnValidate() {
        knockbackImpulse = Mathf.Max(0f, knockbackImpulse);
        steeringPause = Mathf.Max(0f, steeringPause);
    }

    private void HandleDamaged(DamageInfo hit) {
        if (!hit.sourcePos.HasValue || knockbackImpulse <= 0f) return;
        if (rb.bodyType != RigidbodyType2D.Dynamic) return;

        Vector2 away = rb.position - hit.sourcePos.Value;
        if (away.sqrMagnitude < 0.0001f) return;
        away.Normalize();

        // Drop any velocity heading into the source so the push isn't eaten by the chase speed
        float inbound = Vector2.Dot(rb.linearVelocity, away);
        if (inbound < 0f) rb.linearVelocity -= away * inbound;

        rb.AddForce(away * knockbackImpulse, ForceMode2D.Impulse);
        if (mover != null) mover.PauseSteering(steeringPause);
    }
}
