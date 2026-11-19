using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovementModule : SpaceshipModule {
    [Header("Movement Settings")] [SerializeField]
    float maxSpeed = 8f;

    Rigidbody2D rb;

    void Awake() {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate() {
        Vector2 forceNeeded = currentMoveInput * (maxSpeed * rb.mass * Time.fixedDeltaTime);
        rb.AddForce(forceNeeded, ForceMode2D.Force);
    }
}
