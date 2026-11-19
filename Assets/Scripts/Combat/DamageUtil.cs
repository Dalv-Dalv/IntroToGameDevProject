using UnityEngine;

public static class DamageUtil {
    // The one rule every damage dealer uses: the collider's Rigidbody2D owner first, else the nearest parent
    public static bool TryGetDamageable(Collider2D col, out IDamageable target) {
        target = null;
        if (col == null) return false;

        var body = col.attachedRigidbody;
        if (body != null && body.TryGetComponent(out target)) return true;

        target = col.GetComponentInParent<IDamageable>();
        return target != null;
    }
}
