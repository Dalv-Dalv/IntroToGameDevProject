using UnityEngine;

// Engaged: fly at the ship and hover near its hull.
// Heads for the ship's center (stable direction), speed eases off by distance to the hull so it doesn't shove the ship.
// stopDistance ~0 = rammer (pair with a contact-damage component).
public class ApproachBehavior : EnemyBehavior {
    [SerializeField] private float speed = 6f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float stopDistance = 1f;     // hover this far from the hull (enemy radius ~0.6)
    [SerializeField] private float slowDownDistance = 3f; // ease off over this distance before stopping

    void OnValidate() {
        speed = Mathf.Max(0f, speed);
        acceleration = Mathf.Max(0f, acceleration);
        stopDistance = Mathf.Max(0f, stopDistance);
        slowDownDistance = Mathf.Max(0.01f, slowDownDistance);
    }

    public override void TickEngaged() {
        Vector2 toShip = Brain.Ship.position - Body.position;
        float centerDist = toShip.magnitude;
        if (centerDist < 0.001f) {
            Mover.Stop(acceleration);
            return;
        }

        float ease = Mathf.Clamp01((Brain.HullDistance - stopDistance) / slowDownDistance);
        Mover.SetDesiredVelocity(toShip / centerDist * (speed * ease), acceleration);
    }
}
