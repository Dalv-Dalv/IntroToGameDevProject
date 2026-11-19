using UnityEngine;

// Pans up and down to follow a target
[RequireComponent(typeof(Camera))]
public class MapCamera : MonoBehaviour {
    [SerializeField] private float size = 5f;
    [SerializeField] private float smoothTime = 0.3f;
    [Tooltip("How far above the target the camera looks")]
    [SerializeField] private float lookAhead = 3f;

    private Transform target;
    private float velocity;

    public void SetTarget(Transform newTarget) {
        target = newTarget;

        var cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = size;
        transform.position = new Vector3(0f, target.position.y + lookAhead, -10f);
    }

    void LateUpdate() {
        if (target == null) return;

        var position = transform.position;
        position.y = Mathf.SmoothDamp(position.y, target.position.y + lookAhead, ref velocity, smoothTime);
        transform.position = position;
    }
}
