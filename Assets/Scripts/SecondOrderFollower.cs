using UnityEngine;

public class SecondOrderFollower : MonoBehaviour {
    [SerializeField] private Transform target;
    [SerializeField] private DynamicsParams dynamics = new DynamicsParams(3f, 0.6f, 0f);

    private V3SecondOrderDynamics positionDynamics;
    private Vector3 previousTargetPosition;

    private void Start() {
        if (target == null) {
            Debug.LogError("No target assigned to VisualFollower!", this);
            return;
        }

        previousTargetPosition = target.position;
        positionDynamics = new V3SecondOrderDynamics(dynamics.f, dynamics.z, dynamics.r, transform.position);
    }

    private void LateUpdate() {
        if (!target) return;

        var targetVelocity = (target.position - previousTargetPosition) / Time.deltaTime;
        previousTargetPosition = target.position;

        transform.position = positionDynamics.Update(Time.deltaTime, target.position, targetVelocity);
    }
    
#if UNITY_EDITOR
    private void OnValidate() {
        if (!Application.isPlaying) return;
        positionDynamics.ChangeParameters(Mathf.Max(0.01f, dynamics.f), dynamics.z, dynamics.r);
    }
#endif
}