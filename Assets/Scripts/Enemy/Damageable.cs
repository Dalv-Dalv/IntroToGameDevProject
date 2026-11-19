using System;
using UnityEngine;

// Health. Destroys the GameObject when it reaches 0
public class Damageable : MonoBehaviour, IDamageable {
    [SerializeField] private float maxHealth = 30f;

    public event Action<float, Vector2?> OnDamaged; // amount, sourcePos
    public event Action OnDied;

    public float Health { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => Health <= 0f;

    void Awake() {
        Health = maxHealth;
    }

    public void DealDamage(float amount, Vector2? sourcePos = null) {
        if (IsDead || amount <= 0f) return;

        Health = Mathf.Max(0f, Health - amount);
        OnDamaged?.Invoke(amount, sourcePos);
        if (!IsDead) return;

        OnDied?.Invoke();
        Destroy(gameObject);
    }
}
