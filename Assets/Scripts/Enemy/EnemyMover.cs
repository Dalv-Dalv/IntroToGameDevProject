using UnityEngine;

// Moves the enemy's Rigidbody2D based on EnemyBrain's state.
// Patrol = wander to random points around the spawn, Chase = smoothly pursue the ship.
// Halts (zero velocity) whenever the level isn't Playing or there is no LevelManager.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMover : MonoBehaviour {
    [Header("Patrol")]
    [SerializeField] private float patrolRadius = 4f;
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float acceleration = 6f;
    [SerializeField] private float arriveDistance = 0.4f;
    [SerializeField] private float slowDownDistance = 1.5f;
    [SerializeField] private float maxPatrolTime = 6f; // give up on a point if blocked (e.g. by an obstacle)

    [Header("Chase (distances measured to the ship hull)")]
    [SerializeField] private float chaseSpeed = 6f;
    [SerializeField] private float chaseAcceleration = 10f;
    [SerializeField] private float chaseStopDistance = 1f;      // hover this far from the hull (enemy radius ~0.6)
    [SerializeField] private float chaseSlowDownDistance = 3f;  // ease off over this distance before stopping

    public bool ReachedPatrolPoint { get; private set; }

    private Rigidbody2D rb;
    private EnemyBrain brain;
    private Vector2 spawnPoint;
    private Vector2 patrolPoint;
    private float patrolGiveUpTime;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        brain = GetComponent<EnemyBrain>();
        spawnPoint = rb.position;
        patrolPoint = spawnPoint;
    }

    void OnValidate() {
        chaseSpeed = Mathf.Max(0f, chaseSpeed);
        chaseAcceleration = Mathf.Max(0f, chaseAcceleration);
        chaseStopDistance = Mathf.Max(0f, chaseStopDistance);
        chaseSlowDownDistance = Mathf.Max(0.01f, chaseSlowDownDistance);
    }

    public void PickPatrolPoint() {
        patrolPoint = spawnPoint + Random.insideUnitCircle * patrolRadius;
        patrolGiveUpTime = Time.time + maxPatrolTime;
        ReachedPatrolPoint = false;
    }

    void FixedUpdate() {
        // Level over (or no level at all) -> freeze in place
        var lm = LevelManager.Instance;
        if (lm == null || lm.State != LevelManager.LevelState.Playing) {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        var state = brain != null ? brain.CurrentState : EnemyBrain.State.Idle;

        switch (state) {
            case EnemyBrain.State.Patrol:
                Patrol();
                break;
            case EnemyBrain.State.Chase:
                Chase(lm.Ship);
                break;
            default:
                SteerTowards(Vector2.zero, acceleration);
                break;
        }
    }

    private void Patrol() {
        Vector2 toPoint = patrolPoint - rb.position;
        float dist = toPoint.magnitude;

        if (dist <= arriveDistance || Time.time >= patrolGiveUpTime) {
            ReachedPatrolPoint = true;
            SteerTowards(Vector2.zero, acceleration);
            return;
        }

        // Ease in near the point so it doesn't overshoot
        float speed = patrolSpeed * Mathf.Clamp01(dist / slowDownDistance);
        SteerTowards(toPoint / dist * speed, acceleration);
    }

    private void Chase(Rigidbody2D ship) {
        if (ship == null) {
            SteerTowards(Vector2.zero, chaseAcceleration);
            return;
        }

        // Head for the ship's center (stable direction), but ease off based on distance to the hull
        Vector2 toShip = ship.position - rb.position;
        float centerDist = toShip.magnitude;
        if (centerDist < 0.001f) {
            SteerTowards(Vector2.zero, chaseAcceleration);
            return;
        }

        float hullDist = Vector2.Distance(rb.position, ship.ClosestPoint(rb.position));
        float ease = Mathf.Clamp01((hullDist - chaseStopDistance) / chaseSlowDownDistance);
        SteerTowards(toShip / centerDist * (chaseSpeed * ease), chaseAcceleration);
    }

    // Accelerate the velocity toward the desired one (smooth, capped change per step)
    private void SteerTowards(Vector2 desiredVelocity, float accel) {
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desiredVelocity, accel * Time.fixedDeltaTime);
    }

    void OnDrawGizmosSelected() {
        Vector2 center = Application.isPlaying ? spawnPoint : (Vector2)transform.position;

        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireSphere(center, patrolRadius);

        if (!Application.isPlaying) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(rb.position, patrolPoint);
        Gizmos.DrawWireSphere(patrolPoint, 0.25f);
    }
}
