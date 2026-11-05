using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SpaceshipController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private DynamicsParams movementDynamics = new DynamicsParams(1.5f, 0.8f, 0f);

    private V2SecondOrderDynamics velocityDynamics;
    private Vector2 currentMoveInput;
    private Vector2 previousTargetVelocity;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        velocityDynamics = new V2SecondOrderDynamics(movementDynamics.f, movementDynamics.z, movementDynamics.r, Vector2.zero);
    }

    public void SetMoveInput(Vector2 input)
    {
        currentMoveInput = Vector2.ClampMagnitude(input, 1f);
    }

    private void FixedUpdate()
    {
        velocityDynamics.ChangeParameters(Mathf.Max(0.01f, movementDynamics.f), movementDynamics.z, movementDynamics.r);

        Vector2 targetVelocity = currentMoveInput * maxSpeed;
        Vector2 targetVelocityDerivative = (targetVelocity - previousTargetVelocity) / Time.fixedDeltaTime;
        previousTargetVelocity = targetVelocity;

        Vector2 smoothedVelocity = velocityDynamics.Update(Time.fixedDeltaTime, targetVelocity, targetVelocityDerivative);

        Vector2 velocityDifference = smoothedVelocity - rb.linearVelocity;
        Vector2 forceNeeded = (velocityDifference / Time.fixedDeltaTime) * rb.mass;

        rb.AddForce(forceNeeded, ForceMode2D.Force);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (!Application.isPlaying || velocityDynamics == null) return;
        velocityDynamics.ChangeParameters(Mathf.Max(0.01f, movementDynamics.f), movementDynamics.z, movementDynamics.r);
    }
#endif
}