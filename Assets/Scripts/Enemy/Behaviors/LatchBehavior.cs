using UnityEngine;

// Engaged: fly into the hull and latch on via HullAttacher. While attached the brain keeps aggro and the body rides the
// ship; whatever else is on the prefab (e.g. ContactDamage) does the harm. If it can't attach (limit reached / no room)
// it hovers nearby and retries; after being shaken off it waits a bit before trying again.
[RequireComponent(typeof(HullAttacher))]
public class LatchBehavior : EnemyBehavior {
    [SerializeField] private float speed = 6f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float attachReach = 0.25f;  // try to latch when the gap to the hull is this small
    [SerializeField] private float waitDistance = 2.5f;  // hover this far from the hull while it can't attach
    [SerializeField] private float retryInterval = 1f;   // after a failed attempt
    [SerializeField] private float reattachDelay = 2f;   // after being detached

    private HullAttacher attacher;
    private float nextAttemptTime;

    public override bool HoldAggro => attacher.IsAttached;

    protected override void Awake() {
        base.Awake();
        attacher = GetComponent<HullAttacher>();
    }

    void OnEnable() {
        attacher.OnDetached += HandleDetached;
    }

    void OnDisable() {
        attacher.OnDetached -= HandleDetached;
    }

    void OnValidate() {
        speed = Mathf.Max(0f, speed);
        acceleration = Mathf.Max(0f, acceleration);
        attachReach = Mathf.Max(0f, attachReach);
        waitDistance = Mathf.Max(0f, waitDistance);
        retryInterval = Mathf.Max(0f, retryInterval);
        reattachDelay = Mathf.Max(0f, reattachDelay);
    }

    private void HandleDetached() {
        nextAttemptTime = Time.time + reattachDelay;
    }

    public override void TickEngaged() {
        if (attacher.IsAttached) return; // riding the ship; the mover is off

        Vector2 toShip = Brain.Ship.position - Body.position;
        if (toShip.sqrMagnitude < 0.0001f) return;

        bool waiting = Time.time < nextAttemptTime;
        float gap = Brain.HullDistance - attacher.BodyRadius;

        if (!waiting && gap <= attachReach) {
            if (!attacher.TryAttach(Brain.Ship)) nextAttemptTime = Time.time + retryInterval;
            return;
        }

        // Close in (never fully stalling) or, while waiting, hold waitDistance (backing off if closer)
        float ease = waiting
            ? Mathf.Clamp((gap - waitDistance) / 1.5f, -1f, 1f)
            : Mathf.Max(0.3f, Mathf.Clamp01(gap / 1.5f));
        Mover.SetDesiredVelocity(toShip.normalized * (speed * ease), acceleration);
    }
}
