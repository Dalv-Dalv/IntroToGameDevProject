using UnityEngine;

public class SquashAndStretch : MonoBehaviour {
    [Header("References")] [SerializeField]
    Rigidbody2D rb;

    [Header("Deformation Settings")] [Tooltip("How much the sprite deforms per unit of velocity change.")] [SerializeField]
    float deformationMultiplier = 0.05f;

    [Tooltip("The maximum amount the scale can stretch or squash to prevent it from vanishing.")] [SerializeField]
    float maxDeformation = 0.5f;

    [Header("Bounce Dynamics")] [SerializeField]
    DynamicsParams dynamics = new DynamicsParams(5f, 0.25f, 0f);

    Vector2 previousVelocity;

    V3SecondOrderDynamics scaleDynamics;
    Vector3 targetScale = Vector3.one;

    void Start() {
        if (rb == null) {
            Debug.LogError("Missing references on SquashAndStretch!", this);
            return;
        }

        scaleDynamics = new V3SecondOrderDynamics(dynamics.f, dynamics.z, dynamics.r, transform.localScale);
        previousVelocity = rb.linearVelocity;
    }

    void FixedUpdate() {
        float deltaSpeedX = Mathf.Abs(rb.linearVelocity.x) - Mathf.Abs(previousVelocity.x);
        float deltaSpeedY = Mathf.Abs(rb.linearVelocity.y) - Mathf.Abs(previousVelocity.y);

        previousVelocity = rb.linearVelocity;

        float xDeformation = deltaSpeedX * deformationMultiplier;
        float yDeformation = deltaSpeedY * deformationMultiplier;

        float scaleX = 1f + xDeformation - yDeformation;
        float scaleY = 1f + yDeformation - xDeformation;

        scaleX = Mathf.Clamp(scaleX, 1f - maxDeformation, 1f + maxDeformation);
        scaleY = Mathf.Clamp(scaleY, 1f - maxDeformation, 1f + maxDeformation);

        targetScale = new Vector3(scaleX, scaleY, 1f);
    }

    void LateUpdate() {
        transform.localScale = scaleDynamics.Update(Time.deltaTime, targetScale, Vector3.zero);
    }

#if UNITY_EDITOR
    void OnValidate() {
        if (!Application.isPlaying) return;
        if (scaleDynamics == null) return;
        scaleDynamics.ChangeParameters(Mathf.Max(0.01f, dynamics.f), dynamics.z, dynamics.r);
    }
#endif
}
