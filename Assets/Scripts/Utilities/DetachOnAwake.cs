using UnityEngine;

public class DetachOnAwake : MonoBehaviour {
    [Tooltip("If true, the object keeps its world position/rotation/scale when detached.")] [SerializeField]
    bool keepWorldTransform = true;

    void Awake() {
        transform.SetParent(null, keepWorldTransform);
    }
}
