using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ShipCrashDetector : MonoBehaviour {
    [SerializeField] private LayerMask crashLayers = ~0;

    void OnCollisionEnter2D(Collision2D c) {
        var level = LevelManager.Instance;
        if (level == null || level.IsInGracePeriod) return;
        if ((crashLayers.value & (1 << c.collider.gameObject.layer)) == 0) return;
        level.Fail();
    }
}
