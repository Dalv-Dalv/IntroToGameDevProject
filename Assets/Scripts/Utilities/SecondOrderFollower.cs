using UnityEngine;

public class SecondOrderFollower : MonoBehaviour {
    [SerializeField] Transform target;
    [SerializeField] DynamicsParams dynamics = new DynamicsParams(3f, 0.6f, 0f);

    V3SecondOrderDynamics positionDynamics;
    Vector3 previousTargetPosition;

    void Start() {
        if (target == null) {
            Debug.LogError("No target assigned to SecondOrderFollower!", this);
            return;
        }

        previousTargetPosition = GetTargetPositionWithCurrentZ();
        positionDynamics = new V3SecondOrderDynamics(dynamics.f, dynamics.z, dynamics.r, transform.position);
    }

    void LateUpdate() {
        if (!target) return;

        Vector3 targetPos = GetTargetPositionWithCurrentZ();
        Vector3 targetVelocity = (targetPos - previousTargetPosition) / Time.deltaTime;
        previousTargetPosition = targetPos;

        transform.position = positionDynamics.Update(Time.deltaTime, targetPos, targetVelocity);
    }

#if UNITY_EDITOR
    void OnValidate() {
        if (!Application.isPlaying || positionDynamics == null) return;
        positionDynamics.ChangeParameters(Mathf.Max(0.01f, dynamics.f), dynamics.z, dynamics.r);
    }
#endif

    Vector3 GetTargetPositionWithCurrentZ() {
        return new Vector3(target.position.x, target.position.y, transform.position.z);
    }
}
