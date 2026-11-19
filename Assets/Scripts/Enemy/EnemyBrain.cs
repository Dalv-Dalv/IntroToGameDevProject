using System;
using System.Collections.Generic;
using UnityEngine;

// Decides what the enemy is doing: Idle (waiting) <-> Patrol (wandering) -> Chase (ship spotted)
[RequireComponent(typeof(EnemyMover))]
public class EnemyBrain : MonoBehaviour {
    public enum State { Idle, Patrol, Chase }

    [Header("Detection (measured to the ship hull)")]
    [SerializeField] private float detectRadius = 8f;
    [SerializeField] private float loseRadius = 13f;
    [SerializeField] private bool useLineOfSight = true;
    [SerializeField] private LayerMask lineOfSightMask = (1 << 0) | (1 << 6); // Default (obstacles) + Ground (hull)

    [Header("Idle")]
    [SerializeField] private Vector2 idleTimeRange = new Vector2(1f, 2.5f);

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = true;

    public event Action<State, State> OnStateChanged; // from, to

    public State CurrentState { get; private set; } = State.Idle;
    public float DetectRadius => detectRadius;

    private EnemyMover mover;
    private float idleUntil;
    private bool lastSightClear; // for gizmos
    private readonly List<RaycastHit2D> losHits = new List<RaycastHit2D>();

    void Awake() {
        mover = GetComponent<EnemyMover>();
    }

    void Start() {
        EnterState(State.Idle);
    }

    void OnValidate() {
        detectRadius = Mathf.Max(0f, detectRadius);
        loseRadius = Mathf.Max(detectRadius, loseRadius);
    }

    void FixedUpdate() {
        var ship = GetShip();
        float shipDist = ship != null ? DistanceToShip(ship) : Mathf.Infinity;

        switch (CurrentState) {
            case State.Idle:
                if (CanSeeShip(ship, shipDist)) EnterState(State.Chase);
                else if (Time.time >= idleUntil) EnterState(State.Patrol);
                break;

            case State.Patrol:
                if (CanSeeShip(ship, shipDist)) EnterState(State.Chase);
                else if (mover.ReachedPatrolPoint) EnterState(State.Idle);
                break;

            case State.Chase:
                // Once aggroed, only distance (or level end) drops it, not LOS
                if (ship == null || shipDist > loseRadius) EnterState(State.Patrol);
                break;
        }
    }

    private void EnterState(State next) {
        State prev = CurrentState;
        CurrentState = next;

        if (next == State.Idle) idleUntil = Time.time + UnityEngine.Random.Range(idleTimeRange.x, idleTimeRange.y);
        if (next == State.Patrol) mover.PickPatrolPoint();

        if (prev == next) return;
        if (logStateChanges) Debug.Log($"[EnemyBrain] {name}: {prev} -> {next}", this);
        OnStateChanged?.Invoke(prev, next);
    }

    // Ship only counts while the level is running
    private static Rigidbody2D GetShip() {
        var lm = LevelManager.Instance;
        if (lm == null || lm.State != LevelManager.LevelState.Playing) return null;
        return lm.Ship;
    }

    // Distance to the nearest point of the hull (the ship root is ~16x11 units)
    private float DistanceToShip(Rigidbody2D ship) {
        Vector2 pos = transform.position;
        return Vector2.Distance(pos, ship.ClosestPoint(pos));
    }

    private bool CanSeeShip(Rigidbody2D ship, float shipDist) {
        if (ship == null || shipDist > detectRadius) return false;
        if (!useLineOfSight) return true;

        lastSightClear = HasLineOfSight(ship);
        return lastSightClear;
    }

    // Visible if the first solid thing on the line to the ship's center is the ship itself
    private bool HasLineOfSight(Rigidbody2D ship) {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(lineOfSightMask);

        int count = Physics2D.Linecast(transform.position, ship.position, filter, losHits);
        if (count == 0) return true;

        RaycastHit2D nearest = losHits[0];
        for (int i = 1; i < count; i++) {
            if (losHits[i].distance < nearest.distance) nearest = losHits[i];
        }
        return nearest.rigidbody == ship;
    }

    void OnDrawGizmos() {
        Vector3 pos = transform.position;

        Gizmos.color = StateColor(CurrentState);
        Gizmos.DrawWireSphere(pos, detectRadius);

        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(pos, loseRadius);

        if (!Application.isPlaying) return;
        var ship = GetShip();
        if (ship == null) return;
        float range = CurrentState == State.Chase ? loseRadius : detectRadius;
        if (DistanceToShip(ship) > range) return;

        // Line to the ship: red = chasing, green/orange = LOS clear/blocked
        if (CurrentState == State.Chase) Gizmos.color = Color.red;
        else Gizmos.color = (!useLineOfSight || lastSightClear) ? Color.green : new Color(1f, 0.5f, 0f);
        Gizmos.DrawLine(pos, ship.position);
    }

    private static Color StateColor(State s) {
        switch (s) {
            case State.Patrol: return Color.yellow;
            case State.Chase: return Color.red;
            default: return Color.gray;
        }
    }
}
