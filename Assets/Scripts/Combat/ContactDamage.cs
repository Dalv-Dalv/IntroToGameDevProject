using System;
using System.Collections.Generic;
using UnityEngine;

// Damages IDamageables it touches - solid contact or trigger overlap - at most once per cooldown per target.
// Generic: rammers, spiky hazards, enemies latched onto the hull. Only hits colliders on targetLayers, never its own body.
// Does nothing while the level isn't Playing.
public class ContactDamage : MonoBehaviour {
    [SerializeField] private float damage = 10f;
    [SerializeField] private DamageType damageType = DamageType.Contact;
    [SerializeField] private LayerMask targetLayers = 1 << 8; // Spaceship (hull)
    [SerializeField] private float cooldown = 0.5f;           // per target

    public event Action<IDamageable, DamageInfo> OnHit; // target, hit (e.g. for recoil / sounds)

    public float Damage => damage;
    public float Cooldown => cooldown;

    private Rigidbody2D ownBody;
    private readonly Dictionary<IDamageable, float> nextHitTime = new Dictionary<IDamageable, float>();

    void Awake() {
        ownBody = GetComponentInParent<Rigidbody2D>();
    }

    void OnValidate() {
        damage = Mathf.Max(0f, damage);
        cooldown = Mathf.Max(0f, cooldown);
    }

    void OnCollisionEnter2D(Collision2D c) {
        TryHit(c.collider);
    }

    void OnCollisionStay2D(Collision2D c) {
        TryHit(c.collider);
    }

    void OnTriggerEnter2D(Collider2D other) {
        TryHit(other);
    }

    void OnTriggerStay2D(Collider2D other) {
        TryHit(other);
    }

    private void TryHit(Collider2D other) {
        // Physics messages also reach disabled components
        if (!enabled || damage <= 0f || !LevelGate.IsPlaying) return;
        if ((targetLayers.value & (1 << other.gameObject.layer)) == 0) return;
        if (ownBody != null && other.attachedRigidbody == ownBody) return;
        if (!DamageUtil.TryGetDamageable(other, out var target)) return;

        if (nextHitTime.TryGetValue(target, out float next) && Time.time < next) return;
        nextHitTime[target] = Time.time + cooldown;

        var hit = new DamageInfo(damage, damageType, transform.position, gameObject);
        target.DealDamage(hit);
        OnHit?.Invoke(target, hit);
    }
}
