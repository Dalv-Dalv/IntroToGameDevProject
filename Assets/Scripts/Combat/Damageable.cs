using System;
using UnityEngine;

// Universal health for anything that can be hit (enemies, destructibles, the ship).
// Per-object differences are inspector data: accepted damage types, destroy on death, invulnerability after a hit.
[DisallowMultipleComponent]
public class Damageable : MonoBehaviour, IDamageable {
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private DamageType acceptedTypes = (DamageType)~0; // hits of other types are blocked
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float invulnerabilityTime = 0f; // after a hit, further hits are blocked this long

    public event Action<DamageInfo> OnDamaged;        // HP went down
    public event Action<DamageInfo> OnDamageBlocked;  // wrong type or invulnerable (nothing fires once dead)
    public event Action<float, float> OnHealthChanged; // current, max
    public event Action OnDied;                       // once, before Destroy

    public float Health { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => Health <= 0f;
    public DamageType AcceptedTypes => acceptedTypes;

    private float invulnerableUntil = -1f;

    void Awake() {
        Health = maxHealth;
    }

    void OnValidate() {
        maxHealth = Mathf.Max(0.01f, maxHealth);
        invulnerabilityTime = Mathf.Max(0f, invulnerabilityTime);
    }

    public bool Accepts(DamageType type) {
        return (acceptedTypes & type) != 0;
    }

    public void DealDamage(DamageInfo hit) {
        if (IsDead || hit.amount <= 0f) return;

        if (!Accepts(hit.type) || Time.time < invulnerableUntil) {
            OnDamageBlocked?.Invoke(hit);
            return;
        }

        Health = Mathf.Max(0f, Health - hit.amount);
        if (invulnerabilityTime > 0f) invulnerableUntil = Time.time + invulnerabilityTime;

        OnHealthChanged?.Invoke(Health, maxHealth);
        OnDamaged?.Invoke(hit);
        if (IsDead) Die();
    }

    public void Heal(float amount) {
        if (IsDead || amount <= 0f || Health >= maxHealth) return;
        Health = Mathf.Min(maxHealth, Health + amount);
        OnHealthChanged?.Invoke(Health, maxHealth);
    }

    // Instant death, ignores accepted types and invulnerability
    public void Kill() {
        if (IsDead) return;
        Health = 0f;
        OnHealthChanged?.Invoke(Health, maxHealth);
        Die();
    }

    private void Die() {
        OnDied?.Invoke();
        if (destroyOnDeath) Destroy(gameObject);
    }
}
