using System;
using System.Collections.Generic;
using UnityEngine;

// Perception + mode for an enemy: Idle (waiting) <-> Patrol (wandering around home) -> Engaged (ship spotted).
// What the enemy does while Engaged is up to its EnemyBehavior component; the brain knows no enemy types.
// Runs before EnemyMover so its commands apply in the same physics step.
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(EnemyMover))]
public class EnemyBrain : MonoBehaviour {
    public enum State { Idle, Patrol, Engaged }

    [Header("Detection (measured to the ship hull)")]
    [SerializeField] private float detectRadius = 8f;
    [SerializeField] private float loseRadius = 13f;
    [SerializeField] private bool useLineOfSight = true;
    [SerializeField] private LayerMask lineOfSightMask = (1 << 0) | (1 << 6) | (1 << 8); // Default (obstacles) + Ground + Spaceship (hull)

    [Header("Idle / Patrol (around the spawn point)")]
    [SerializeField] private Vector2 idleTimeRange = new Vector2(1f, 2.5f);
    [SerializeField] private float patrolRadius = 4f;
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float maxPatrolTime = 6f; // give up on a point if blocked (e.g. by an obstacle)

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;

    public event Action<State, State> OnStateChanged; // from, to

    public State CurrentState { get; private set; } = State.Idle;
    public float DetectRadius => detectRadius;
    public float LoseRadius => loseRadius;
    public Vector2 Home { get; private set; }
    public EnemyMover Mover => mover;

    // Refreshed every FixedUpdate. Ship is null unless the level is Playing
    public Rigidbody2D Ship { get; private set; }
    public float HullDistance { get; private set; } = Mathf.Infinity;
    public Vector2 ClosestHullPoint { get; private set; }

    private EnemyMover mover;
    private EnemyBehavior behavior;
    private Rigidbody2D rb;
    private float idleUntil;
    private float patrolGiveUpTime;
    private bool lastSightClear; // for gizmos
    private readonly List<RaycastHit2D> losHits = new List<RaycastHit2D>();

    void Awake() {
        mover = GetComponent<EnemyMover>();
        behavior = GetComponent<EnemyBehavior>();
        rb = GetComponent<Rigidbody2D>();
        Home = rb.position;
    }

    void Start() {
        EnterState(State.Idle);
    }

    void OnValidate() {
        detectRadius = Mathf.Max(0f, detectRadius);
        loseRadius = Mathf.Max(detectRadius, loseRadius);
        patrolRadius = Mathf.Max(0f, patrolRadius);
    }

    void FixedUpdate() {
        SenseShip();

        switch (CurrentState) {
            case State.Idle:
                if (CanSeeShip()) EnterState(State.Engaged);
                else if (Time.time >= idleUntil) EnterState(State.Patrol);
                break;

            case State.Patrol:
                if (CanSeeShip()) EnterState(State.Engaged);
                else if (mover.HasArrived || Time.time >= patrolGiveUpTime) EnterState(State.Idle);
                break;

            case State.Engaged:
                // Once aggroed, only distance (or level end) drops it, not LOS. The behavior may hold aggro.
                bool holdAggro = behavior != null && behavior.HoldAggro;
                if (Ship == null || (HullDistance > loseRadius && !holdAggro)) EnterState(State.Patrol);
                else if (behavior != null) behavior.TickEngaged();
                break;
        }
    }

    private void SenseShip() {
        Ship = LevelGate.Ship;
        if (Ship == null) {
            HullDistance = Mathf.Infinity;
            return;
        }

        // Distance to the nearest point of the hull (root CircleCollider2D, r ~9.65), not the ship's center
        Vector2 pos = rb.position;
        ClosestHullPoint = Ship.ClosestPoint(pos);
        HullDistance = Vector2.Distance(pos, ClosestHullPoint);
    }

    private void EnterState(State next) {
        State prev = CurrentState;
        if (prev == State.Engaged && next != State.Engaged && behavior != null) behavior.OnDisengage();
        CurrentState = next;

        switch (next) {
            case State.Idle:
                idleUntil = Time.time + UnityEngine.Random.Range(idleTimeRange.x, idleTimeRange.y);
                mover.Stop();
                break;
            case State.Patrol:
                mover.MoveTo(Home + UnityEngine.Random.insideUnitCircle * patrolRadius, patrolSpeed);
                patrolGiveUpTime = Time.time + maxPatrolTime;
                break;
            case State.Engaged:
                if (prev == State.Engaged) break;
                if (behavior != null) behavior.OnEngage();
                else mover.Stop();
                break;
        }

        if (prev == next) return;
        if (logStateChanges) Debug.Log($"[EnemyBrain] {name}: {prev} -> {next}", this);
        OnStateChanged?.Invoke(prev, next);
    }

    private bool CanSeeShip() {
        if (Ship == null || HullDistance > detectRadius) return false;
        if (!useLineOfSight) return true;

        lastSightClear = HasLineOfSight();
        return lastSightClear;
    }

    // Visible if the first solid thing on the line to the ship's center is the ship itself
    public bool HasLineOfSight() {
        if (Ship == null) return false;

        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(lineOfSightMask);

        int count = Physics2D.Linecast(rb.position, Ship.position, filter, losHits);
        if (count == 0) return true;

        RaycastHit2D nearest = losHits[0];
        for (int i = 1; i < count; i++) {
            if (losHits[i].distance < nearest.distance) nearest = losHits[i];
        }
        return nearest.rigidbody == Ship;
    }

    void OnDrawGizmos() {
        Vector3 pos = transform.position;

        Gizmos.color = StateColor(CurrentState);
        Gizmos.DrawWireSphere(pos, detectRadius);

        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(pos, loseRadius);

        if (!Application.isPlaying || Ship == null) return;
        float range = CurrentState == State.Engaged ? loseRadius : detectRadius;
        if (HullDistance > range) return;

        // Line to the ship: red = engaged, green/orange = LOS clear/blocked
        if (CurrentState == State.Engaged) Gizmos.color = Color.red;
        else Gizmos.color = (!useLineOfSight || lastSightClear) ? Color.green : new Color(1f, 0.5f, 0f);
        Gizmos.DrawLine(pos, Ship.position);
    }

    void OnDrawGizmosSelected() {
        Vector2 center = Application.isPlaying ? Home : (Vector2)transform.position;

        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireSphere(center, patrolRadius);

        if (!Application.isPlaying || mover == null || !mover.IsMovingToPoint) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, mover.TargetPoint);
        Gizmos.DrawWireSphere(mover.TargetPoint, 0.25f);
    }

    private static Color StateColor(State s) {
        switch (s) {
            case State.Patrol: return Color.yellow;
            case State.Engaged: return Color.red;
            default: return Color.gray;
        }
    }
}
