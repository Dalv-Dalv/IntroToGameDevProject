using UnityEngine;

// Dumb steering motor for an enemy's Rigidbody2D: others (EnemyBrain, behaviors) tell it what to do.
// Smoothly accelerates linearVelocity toward the commanded velocity. Halts whenever the level isn't Playing.
// Disable the component to hand the body to something else (e.g. latching onto the hull).
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMover : MonoBehaviour {
    [SerializeField] private float acceleration = 6f;      // used when a command doesn't pass its own
    [SerializeField] private float arriveDistance = 0.4f;  // MoveTo counts as arrived inside this
    [SerializeField] private float slowDownDistance = 1.5f; // MoveTo eases in over this distance

    private enum Mode { Stop, Velocity, Point }

    private Rigidbody2D rb;
    private Mode mode = Mode.Stop;
    private Vector2 desiredVelocity;
    private Vector2 targetPoint;
    private float targetSpeed;
    private float commandAccel;
    private float steeringPausedUntil = -1f;

    public Rigidbody2D Body => rb;
    public Vector2 TargetPoint => targetPoint;
    public bool IsMovingToPoint => mode == Mode.Point;
    public bool HasArrived { get; private set; }

    // True while steering is paused (e.g. knockback) - physics + damping move the body instead
    public bool IsSteeringPaused => Time.time < steeringPausedUntil;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        commandAccel = acceleration;
    }

    void OnValidate() {
        acceleration = Mathf.Max(0f, acceleration);
        arriveDistance = Mathf.Max(0f, arriveDistance);
        slowDownDistance = Mathf.Max(0.01f, slowDownDistance);
    }

    // accel < 0 = use the default acceleration
    public void Stop(float accel = -1f) {
        mode = Mode.Stop;
        commandAccel = accel < 0f ? acceleration : accel;
    }

    public void SetDesiredVelocity(Vector2 velocity, float accel = -1f) {
        mode = Mode.Velocity;
        desiredVelocity = velocity;
        commandAccel = accel < 0f ? acceleration : accel;
    }

    public void MoveTo(Vector2 point, float speed, float accel = -1f) {
        mode = Mode.Point;
        targetPoint = point;
        targetSpeed = speed;
        commandAccel = accel < 0f ? acceleration : accel;
        HasArrived = false;
    }

    // Stop steering for a while so an external push (knockback) can play out
    public void PauseSteering(float seconds) {
        steeringPausedUntil = Mathf.Max(steeringPausedUntil, Time.time + seconds);
    }

    void FixedUpdate() {
        // Level over (or no level at all) -> freeze in place
        if (!LevelGate.IsPlaying) {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (IsSteeringPaused) return;

        switch (mode) {
            case Mode.Velocity:
                SteerTowards(desiredVelocity);
                break;
            case Mode.Point:
                SteerToPoint();
                break;
            default:
                SteerTowards(Vector2.zero);
                break;
        }
    }

    private void SteerToPoint() {
        Vector2 toPoint = targetPoint - rb.position;
        float dist = toPoint.magnitude;

        if (dist <= arriveDistance) {
            HasArrived = true;
            SteerTowards(Vector2.zero);
            return;
        }

        // Ease in near the point so it doesn't overshoot
        float speed = targetSpeed * Mathf.Clamp01(dist / slowDownDistance);
        SteerTowards(toPoint / dist * speed);
    }

    // Accelerate the velocity toward the desired one (smooth, capped change per step)
    private void SteerTowards(Vector2 velocity) {
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, velocity, commandAccel * Time.fixedDeltaTime);
    }
}
