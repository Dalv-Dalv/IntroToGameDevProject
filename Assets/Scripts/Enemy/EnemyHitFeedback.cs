using UnityEngine;

// Hit feedback: on Damageable.OnDamaged, flash the sprite and knock the enemy away from the source.
// Knockback pauses EnemyMover's steering briefly so the impulse isn't instantly cancelled by the chase.
[RequireComponent(typeof(Damageable), typeof(Rigidbody2D))]
public class EnemyHitFeedback : MonoBehaviour {
    [Header("Flash")]
    [SerializeField] private SpriteRenderer spriteRenderer; // auto-found in children if empty
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;

    [Header("Knockback (only when the hit has a sourcePos)")]
    [SerializeField] private float knockbackImpulse = 7f;
    [SerializeField] private float steeringPause = 0.3f; // mover stops steering for this long after a hit

    private Damageable damageable;
    private Rigidbody2D rb;
    private EnemyMover mover;
    private Color baseColor;
    private float flashUntil = -1f;

    void Awake() {
        damageable = GetComponent<Damageable>();
        rb = GetComponent<Rigidbody2D>();
        mover = GetComponent<EnemyMover>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;
    }

    void OnEnable() {
        damageable.OnDamaged += HandleDamaged;
    }

    void OnDisable() {
        damageable.OnDamaged -= HandleDamaged;
        EndFlash();
    }

    void OnValidate() {
        flashDuration = Mathf.Max(0f, flashDuration);
        knockbackImpulse = Mathf.Max(0f, knockbackImpulse);
        steeringPause = Mathf.Max(0f, steeringPause);
    }

    void Update() {
        if (flashUntil >= 0f && Time.time >= flashUntil) EndFlash();
    }

    private void HandleDamaged(float amount, Vector2? sourcePos) {
        Flash();
        if (sourcePos.HasValue) Knockback(sourcePos.Value);
    }

    private void Flash() {
        if (spriteRenderer == null || flashDuration <= 0f) return;
        spriteRenderer.color = flashColor;
        flashUntil = Time.time + flashDuration; // repeated hits just extend it
    }

    private void EndFlash() {
        flashUntil = -1f;
        if (spriteRenderer != null) spriteRenderer.color = baseColor;
    }

    private void Knockback(Vector2 sourcePos) {
        Vector2 away = rb.position - sourcePos;
        if (away.sqrMagnitude < 0.0001f || knockbackImpulse <= 0f) return;
        away.Normalize();

        // Drop any velocity heading into the source so the push isn't eaten by the chase speed
        float inbound = Vector2.Dot(rb.linearVelocity, away);
        if (inbound < 0f) rb.linearVelocity -= away * inbound;

        rb.AddForce(away * knockbackImpulse, ForceMode2D.Impulse);
        if (mover != null) mover.PauseSteering(steeringPause);
    }
}
