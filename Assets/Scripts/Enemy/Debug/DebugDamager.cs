using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Test only: stands in for weapons. Press the key to damage every IDamageable within the radius.
// Never hits its own body (it sits on the ship). Add several with different keys/types to test filtering.
public class DebugDamager : MonoBehaviour {
    [SerializeField] private Key damageKey = Key.K;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float radius = 6f;
    [SerializeField] private DamageType damageType = DamageType.Generic;

    private Rigidbody2D ownBody;

    void Awake() {
        ownBody = GetComponentInParent<Rigidbody2D>();
    }

    void Update() {
        var kb = Keyboard.current;
        if (kb == null || !kb[damageKey].wasPressedThisFrame) return;

        Vector2 center = transform.position;
        var alreadyHit = new HashSet<IDamageable>();

        foreach (var col in Physics2D.OverlapCircleAll(center, radius)) {
            if (ownBody != null && col.attachedRigidbody == ownBody) continue;
            if (!DamageUtil.TryGetDamageable(col, out var target) || !alreadyHit.Add(target)) continue;

            var hp = target as Damageable;
            float before = hp != null ? hp.Health : 0f;

            target.DealDamage(new DamageInfo(damage, damageType, center, gameObject));

            if (hp == null) continue;
            string result = hp.Health < before ? $"{hp.Health}/{hp.MaxHealth}{(hp.IsDead ? " (dead)" : "")}" : "BLOCKED";
            Debug.Log($"[DebugDamager {damageKey}] {damageType} {damage} -> {hp.name}: {result}");
        }

        if (alreadyHit.Count == 0) Debug.Log($"[DebugDamager {damageKey}] Nothing in range");
    }

    void OnDrawGizmos() {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
