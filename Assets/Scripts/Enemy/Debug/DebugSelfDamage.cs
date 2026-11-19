using UnityEngine;
using UnityEngine.InputSystem;

// Test only: stands in for enemy attacks on the ship until they exist.
// Press the key to damage the Damageable on this object; logs HP and death.
[RequireComponent(typeof(Damageable))]
public class DebugSelfDamage : MonoBehaviour {
    [SerializeField] private Key damageKey = Key.H;
    [SerializeField] private float damage = 25f;
    [SerializeField] private DamageType damageType = DamageType.Projectile;

    private Damageable damageable;

    void Awake() {
        damageable = GetComponent<Damageable>();
    }

    void OnEnable() {
        damageable.OnDied += HandleDied;
    }

    void OnDisable() {
        damageable.OnDied -= HandleDied;
    }

    void Update() {
        var kb = Keyboard.current;
        if (kb == null || !kb[damageKey].wasPressedThisFrame) return;

        float before = damageable.Health;
        damageable.DealDamage(new DamageInfo(damage, damageType));

        string result = damageable.Health < before ? $"{damageable.Health}/{damageable.MaxHealth}" : "BLOCKED";
        Debug.Log($"[DebugSelfDamage {damageKey}] {damageType} {damage} -> {name}: {result}");
    }

    private void HandleDied() {
        Debug.Log($"[DebugSelfDamage] {name} died");
    }
}
