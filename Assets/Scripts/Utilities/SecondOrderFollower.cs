using UnityEngine;

public class SecondOrderFollower : MonoBehaviour {
    [SerializeField] private Transform target;
    [SerializeField] private DynamicsParams dynamics = new DynamicsParams(3f, 0.6f, 0f);

    private V3SecondOrderDynamics positionDynamics;
    private Vector3 previousTargetPosition;

    private void Start() {
        if (target == null) {
            Debug.LogError("No target assigned to SecondOrderFollower!", this);
            return;
        }

        previousTargetPosition = GetTargetPositionWithCurrentZ();
        positionDynamics = new V3SecondOrderDynamics(dynamics.f, dynamics.z, dynamics.r, transform.position);
    }

    private void LateUpdate() {
        if (!target) return;

        Vector3 targetPos = GetTargetPositionWithCurrentZ();
        Vector3 targetVelocity = (targetPos - previousTargetPosition) / Time.deltaTime;
        previousTargetPosition = targetPos;

        transform.position = positionDynamics.Update(Time.deltaTime, targetPos, targetVelocity);
    }

    private Vector3 GetTargetPositionWithCurrentZ() {
        return new Vector3(target.position.x, target.position.y, transform.position.z);
    }

#if UNITY_EDITOR
    private void OnValidate() {
        if (!Application.isPlaying || positionDynamics == null) return;
        positionDynamics.ChangeParameters(Mathf.Max(0.01f, dynamics.f), dynamics.z, dynamics.r);
    }
#endif
}
