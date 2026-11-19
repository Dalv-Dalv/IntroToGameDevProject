using System;
using UnityEngine;
using Random = UnityEngine.Random;

// Spawns drops when this Damageable dies (enemies, crates, anything). Each entry rolls its chance once,
// then spawns a random count, scattered outward (Pickup.Launch, or the velocity of a Dynamic body).
[RequireComponent(typeof(Damageable))]
public class DropOnDeath : MonoBehaviour {
    [Serializable]
    public class Drop {
        public GameObject prefab;
        [Range(0f, 1f)] public float chance = 1f;
        public Vector2Int count = new Vector2Int(1, 1); // min, max (inclusive)
    }

    [SerializeField] private Drop[] drops = new Drop[0];
    [SerializeField] private float scatterSpeed = 4f; // each drop flies off at 50-100% of this

    private Damageable damageable;

    void Awake() {
        damageable = GetComponent<Damageable>();
    }

    void OnEnable() {
        damageable.OnDied += SpawnDrops;
    }

    void OnDisable() {
        damageable.OnDied -= SpawnDrops;
    }

    void OnValidate() {
        scatterSpeed = Mathf.Max(0f, scatterSpeed);
        if (drops == null) return;
        foreach (var d in drops) {
            if (d == null) continue;
            d.count.x = Mathf.Max(0, d.count.x);
            d.count.y = Mathf.Max(d.count.x, d.count.y);
        }
    }

    private void SpawnDrops() {
        Vector2 pos = transform.position;

        foreach (var d in drops) {
            if (d == null || d.prefab == null || Random.value > d.chance) continue;

            int n = Random.Range(d.count.x, d.count.y + 1);
            for (int i = 0; i < n; i++) {
                var go = Instantiate(d.prefab, pos, Quaternion.identity);
                Vector2 v = Random.insideUnitCircle.normalized * (scatterSpeed * Random.Range(0.5f, 1f));

                if (go.TryGetComponent(out Pickup pickup)) pickup.Launch(v);
                else if (go.TryGetComponent(out Rigidbody2D body) && body.bodyType == RigidbodyType2D.Dynamic) body.linearVelocity = v;
            }
        }
    }
}
