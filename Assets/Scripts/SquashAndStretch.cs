using UnityEngine;

public class SquashAndStretch : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Deformation Settings")]
    [Tooltip("How much the sprite deforms per unit of velocity change.")]
    [SerializeField] private float deformationMultiplier = 0.05f;
    [Tooltip("The maximum amount the scale can stretch or squash to prevent it from vanishing.")]
    [SerializeField] private float maxDeformation = 0.5f;

    [Header("Bounce Dynamics")]
    [SerializeField] private DynamicsParams dynamics = new DynamicsParams(5f, 0.25f, 0f);

    private V3SecondOrderDynamics scaleDynamics;
    private Vector2 previousVelocity;
    private Vector3 targetScale = Vector3.one;

    private void Start() {
        if (rb == null) {
            Debug.LogError("Missing references on SquashAndStretch!", this);
            return;
        }

        scaleDynamics = new V3SecondOrderDynamics(dynamics.f, dynamics.z, dynamics.r, transform.localScale);
        previousVelocity = rb.linearVelocity;
    }

    private void FixedUpdate() {
        var deltaSpeedX = Mathf.Abs(rb.linearVelocity.x) - Mathf.Abs(previousVelocity.x);
        var deltaSpeedY = Mathf.Abs(rb.linearVelocity.y) - Mathf.Abs(previousVelocity.y);
        
        previousVelocity = rb.linearVelocity;

        var xDeformation = deltaSpeedX * deformationMultiplier;
        var yDeformation = deltaSpeedY * deformationMultiplier;

        var scaleX = 1f + xDeformation - yDeformation;
        var scaleY = 1f + yDeformation - xDeformation;

        scaleX = Mathf.Clamp(scaleX, 1f - maxDeformation, 1f + maxDeformation);
        scaleY = Mathf.Clamp(scaleY, 1f - maxDeformation, 1f + maxDeformation);

        targetScale = new Vector3(scaleX, scaleY, 1f);
    }

    private void LateUpdate() {
        transform.localScale = scaleDynamics.Update(Time.deltaTime, targetScale, Vector3.zero);
    }
    
#if UNITY_EDITOR
    private void OnValidate() {
        if (!Application.isPlaying) return;
        scaleDynamics.ChangeParameters(Mathf.Max(0.01f, dynamics.f), dynamics.z, dynamics.r);
    }
#endif
}