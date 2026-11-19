using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Test only: stands in for weapons. Press the key to damage every IDamageable within the radius
public class DebugDamager : MonoBehaviour {
    [SerializeField] private Key damageKey = Key.K;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float radius = 6f;

    void Update() {
        var kb = Keyboard.current;
        if (kb == null || !kb[damageKey].wasPressedThisFrame) return;

        Vector2 center = transform.position;
        var alreadyHit = new HashSet<IDamageable>();

        foreach (var col in Physics2D.OverlapCircleAll(center, radius)) {
            var target = col.GetComponentInParent<IDamageable>();
            if (target == null || !alreadyHit.Add(target)) continue;

            target.DealDamage(damage, center);

            var hp = target as Damageable;
            if (hp != null) Debug.Log($"[DebugDamager] {hp.name}: {hp.Health}/{hp.MaxHealth}{(hp.IsDead ? " (dead)" : "")}");
        }

        if (alreadyHit.Count == 0) Debug.Log("[DebugDamager] Nothing in range");
    }

    void OnDrawGizmos() {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
