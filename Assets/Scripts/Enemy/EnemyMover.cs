using UnityEngine;

// Moves the enemy's Rigidbody2D based on EnemyBrain's state. Patrol = wander to random points around the spawn
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMover : MonoBehaviour {
    [Header("Patrol")]
    [SerializeField] private float patrolRadius = 4f;
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float acceleration = 6f;
    [SerializeField] private float arriveDistance = 0.4f;
    [SerializeField] private float slowDownDistance = 1.5f;
    [SerializeField] private float maxPatrolTime = 6f; // give up on a point if blocked (e.g. by an obstacle)

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

    public void PickPatrolPoint() {
        patrolPoint = spawnPoint + Random.insideUnitCircle * patrolRadius;
        patrolGiveUpTime = Time.time + maxPatrolTime;
        ReachedPatrolPoint = false;
    }

    void FixedUpdate() {
        var state = brain != null ? brain.CurrentState : EnemyBrain.State.Idle;

        switch (state) {
            case EnemyBrain.State.Patrol:
                Patrol();
                break;
            default:
                // Idle; Chase steering comes in Phase 3, brake for now
                SteerTowards(Vector2.zero);
                break;
        }
    }

    private void Patrol() {
        Vector2 toPoint = patrolPoint - rb.position;
        float dist = toPoint.magnitude;

        if (dist <= arriveDistance || Time.time >= patrolGiveUpTime) {
            ReachedPatrolPoint = true;
            SteerTowards(Vector2.zero);
            return;
        }

        // Ease in near the point so it doesn't overshoot
        float speed = patrolSpeed * Mathf.Clamp01(dist / slowDownDistance);
        SteerTowards(toPoint / dist * speed);
    }

    // Accelerate the velocity toward the desired one (smooth, capped change per step)
    private void SteerTowards(Vector2 desiredVelocity) {
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desiredVelocity, acceleration * Time.fixedDeltaTime);
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
