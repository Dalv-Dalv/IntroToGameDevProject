using UnityEngine;

// Plug-in slot: what an enemy does once EnemyBrain has engaged the ship (approach, kite, latch...).
// One small subclass per enemy idea; the core scripts never know which one is attached.
// Called only while the level is Playing and the ship exists (Brain.Ship != null).
[RequireComponent(typeof(EnemyBrain))]
public abstract class EnemyBehavior : MonoBehaviour {
    protected EnemyBrain Brain { get; private set; }
    protected EnemyMover Mover { get; private set; }
    protected Rigidbody2D Body { get; private set; }
    protected Damageable Damageable { get; private set; } // may be null

    // Subclasses that need Awake must override and call base.Awake()
    protected virtual void Awake() {
        Brain = GetComponent<EnemyBrain>();
        Mover = GetComponent<EnemyMover>();
        Body = GetComponent<Rigidbody2D>();
        Damageable = GetComponent<Damageable>();
    }

    public virtual void OnEngage() { }

    // Every FixedUpdate while Engaged
    public abstract void TickEngaged();

    public virtual void OnDisengage() { }

    // True = brain won't drop aggro by distance (e.g. while latched onto the hull)
    public virtual bool HoldAggro => false;
}
