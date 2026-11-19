using UnityEngine;

// Test only (proves ProjectileLauncher + Projectile, not a final mob).
// Engaged: hold preferredDistance from the hull (backs off if closer) and fire at the hull whenever the launcher is
// ready and there's line of sight.
[RequireComponent(typeof(ProjectileLauncher))]
public class DebugShooterBehavior : EnemyBehavior {
    [SerializeField] private float preferredDistance = 6f; // to the hull
    [SerializeField] private float speed = 4f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private bool requireLineOfSight = true;

    private ProjectileLauncher launcher;

    protected override void Awake() {
        base.Awake();
        launcher = GetComponent<ProjectileLauncher>();
    }

    public override void TickEngaged() {
        Vector2 toShip = Brain.Ship.position - Body.position;
        if (toShip.sqrMagnitude < 0.0001f) return;
        Vector2 dir = toShip.normalized;

        // + = close in, - = back off; full speed when 2+ units off the preferred distance
        float error = Brain.HullDistance - preferredDistance;
        Mover.SetDesiredVelocity(dir * (Mathf.Clamp(error / 2f, -1f, 1f) * speed), acceleration);

        if (!launcher.IsReady) return;
        if (requireLineOfSight && !Brain.HasLineOfSight()) return;
        launcher.TryFire(Brain.ClosestHullPoint - launcher.MuzzlePosition);
    }
}
