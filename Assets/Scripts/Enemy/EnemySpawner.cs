using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Keeps up to maxAlive enemies alive by spawning from a weighted prefab list. Tracks only its own spawns (no manager).
// Area: around this spawner (placed in a level), or a ring around the ship (waves from off-screen).
// Never spawns too close to the hull, inside walls/other bodies, or at Y <= -400 (hidden player-physics rooms).
// Halts while the level isn't Playing.
public class EnemySpawner : MonoBehaviour {
    [Serializable]
    public class Entry {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;
    }

    private const float MinAllowedY = -400f; // PlayerAndSpaceshipSync keeps hidden interiors at Y = -500*n

    [SerializeField] private Entry[] entries = new Entry[0];
    [SerializeField] private int maxAlive = 4;
    [SerializeField] private int totalToSpawn = 0;      // 0 = endless
    [SerializeField] private float initialDelay = 1f;
    [SerializeField] private float spawnInterval = 3f;  // between spawns, and after a slot frees up

    [Header("Where")]
    [SerializeField] private bool aroundShip = false;    // false = within spawnRadius of this spawner
    [SerializeField] private float spawnRadius = 6f;     // around the spawner
    [SerializeField] private Vector2 shipRingDistance = new Vector2(18f, 24f); // from the hull, when aroundShip
    [SerializeField] private float minDistanceFromHull = 8f;
    [SerializeField] private float activationDistance = 0f; // 0 = always; else only while the hull is this close to the spawner
    [SerializeField] private float clearance = 0.8f;     // free radius needed at the spawn point
    [SerializeField] private LayerMask blockingLayers = (1 << 0) | (1 << 6) | (1 << 8) | (1 << 13); // Default, Ground, Spaceship, Enemy
    [SerializeField] private int placementTries = 10;
    [SerializeField] private Transform container;        // optional parent for spawned enemies

    public event Action<GameObject> OnSpawned;

    public int AliveCount { get { Prune(); return alive.Count; } }
    public int SpawnedTotal { get; private set; }
    public bool IsFinished => totalToSpawn > 0 && SpawnedTotal >= totalToSpawn;

    private readonly List<GameObject> alive = new List<GameObject>();
    private readonly List<Collider2D> overlap = new List<Collider2D>();
    private float timer;

    void Awake() {
        timer = initialDelay;
    }

    void OnValidate() {
        maxAlive = Mathf.Max(0, maxAlive);
        totalToSpawn = Mathf.Max(0, totalToSpawn);
        initialDelay = Mathf.Max(0f, initialDelay);
        spawnInterval = Mathf.Max(0.05f, spawnInterval);
        spawnRadius = Mathf.Max(0f, spawnRadius);
        shipRingDistance.x = Mathf.Max(0f, shipRingDistance.x);
        shipRingDistance.y = Mathf.Max(shipRingDistance.x, shipRingDistance.y);
        minDistanceFromHull = Mathf.Max(0f, minDistanceFromHull);
        activationDistance = Mathf.Max(0f, activationDistance);
        clearance = Mathf.Max(0f, clearance);
        placementTries = Mathf.Max(1, placementTries);
    }

    void Update() {
        if (!LevelGate.IsPlaying) return; // paused, timer included

        // Full (or done): hold the timer, so the next spawn comes a full spawnInterval after a slot frees up
        if (IsFinished || AliveCount >= maxAlive) {
            timer = Mathf.Max(timer, spawnInterval);
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        var ship = LevelGate.Ship;
        if (!aroundShip && activationDistance > 0f && (ship == null || HullDistance(ship, transform.position) > activationDistance)) {
            timer = 0.5f;
            return;
        }

        timer = TrySpawn(ship) ? spawnInterval : 0.5f; // no valid spot -> retry soon
    }

    private bool TrySpawn(Rigidbody2D ship) {
        var prefab = PickPrefab();
        if (prefab == null) return false;
        if (aroundShip && ship == null) return false;

        for (int i = 0; i < placementTries; i++) {
            Vector2 pos = aroundShip ? RingPoint(ship) : (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;
            if (!IsValidSpot(ship, pos)) continue;

            var go = Instantiate(prefab, pos, Quaternion.identity, container); // with position: EnemyBrain takes Home in Awake
            alive.Add(go);
            SpawnedTotal++;
            OnSpawned?.Invoke(go);
            return true;
        }
        return false;
    }

    // Random direction, at a random distance from the hull in that direction
    private Vector2 RingPoint(Rigidbody2D ship) {
        Vector2 dir = Random.insideUnitCircle.normalized;
        if (dir == Vector2.zero) dir = Vector2.up;
        Vector2 hullPoint = ship.ClosestPoint(ship.position + dir * 1000f);
        return hullPoint + dir * Random.Range(shipRingDistance.x, shipRingDistance.y);
    }

    private bool IsValidSpot(Rigidbody2D ship, Vector2 pos) {
        if (pos.y <= MinAllowedY) return false;
        if (ship != null && HullDistance(ship, pos) < minDistanceFromHull) return false;

        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(blockingLayers);
        return Physics2D.OverlapCircle(pos, clearance, filter, overlap) == 0;
    }

    private GameObject PickPrefab() {
        float total = 0f;
        foreach (var e in entries) {
            if (e != null && e.prefab != null && e.weight > 0f) total += e.weight;
        }
        if (total <= 0f) return null;

        float roll = Random.value * total;
        foreach (var e in entries) {
            if (e == null || e.prefab == null || e.weight <= 0f) continue;
            roll -= e.weight;
            if (roll <= 0f) return e.prefab;
        }
        return null; // unreachable in practice
    }

    private void Prune() {
        alive.RemoveAll(go => go == null);
    }

    private static float HullDistance(Rigidbody2D ship, Vector2 pos) {
        return Vector2.Distance(pos, ship.ClosestPoint(pos));
    }

    void OnDrawGizmos() {
        Vector3 pos = transform.position;
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
        if (!aroundShip) Gizmos.DrawWireSphere(pos, spawnRadius);
        Gizmos.DrawLine(pos + Vector3.left * 0.5f, pos + Vector3.right * 0.5f);
        Gizmos.DrawLine(pos + Vector3.down * 0.5f, pos + Vector3.up * 0.5f);

        if (!aroundShip && activationDistance > 0f) {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.2f);
            Gizmos.DrawWireSphere(pos, activationDistance); // approx: measured to the hull at runtime
        }
    }
}
