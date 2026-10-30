using UnityEngine;

public class DetachOnAwake : MonoBehaviour {
    [Tooltip("If true, the object keeps its world position/rotation/scale when detached.")]
    [SerializeField] private bool keepWorldTransform = true;

    private void Awake() {
        transform.SetParent(null, keepWorldTransform);
    }
}