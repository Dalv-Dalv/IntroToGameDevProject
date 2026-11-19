using UnityEngine;

public class WeaponFire : SpaceshipModule {
    [SerializeField] float cooldown = 2.0f;
    [SerializeField] float force;
    [SerializeField] float rotationSpeed = 10.0f;
    [SerializeField] Transform centerPoint;

    [SerializeField] float minRotation = -50.0f;
    [SerializeField] float maxRotation = 50.0f;
    [SerializeField] float gizmoRadius = 2.0f;
    [SerializeField] GameObject bullet;

    float currentRotation;
    float currentTime;

    void Start() {
        float initialZ = transform.localEulerAngles.z;
        if (initialZ > 180f) initialZ -= 360f;
        currentRotation = Mathf.Clamp(initialZ, minRotation, maxRotation);
        transform.localRotation = Quaternion.Euler(0f, 0f, currentRotation);
    }

    void Update() {
        currentTime += Time.deltaTime;
        if (currentTime > cooldown && currentInteractInput) {
            GameObject spawnedBullet = Object.Instantiate(bullet, centerPoint.position, Quaternion.identity);
            Rigidbody2D rb = spawnedBullet.GetComponent<Rigidbody2D>();
            rb.AddForce(-centerPoint.transform.right * force, ForceMode2D.Impulse);
            currentTime = 0;
        }

        if (currentMoveInput == Vector2.zero) return;
        Vector2 localInput = transform.parent ? transform.parent.InverseTransformDirection(currentMoveInput) : currentMoveInput;

        float rawAngle = Mathf.Atan2(-localInput.y, -localInput.x) * Mathf.Rad2Deg;
        float midAngle = (minRotation + maxRotation) * 0.5f;
        float targetAngle = midAngle + Mathf.DeltaAngle(midAngle, rawAngle);
        targetAngle = Mathf.Clamp(targetAngle, minRotation, maxRotation);
        currentRotation = Mathf.MoveTowards(currentRotation, targetAngle, rotationSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(0f, 0f, currentRotation);
    }

    void OnDrawGizmosSelected() {
        Vector3 origin = centerPoint != null ? centerPoint.position : transform.position;

        Quaternion parentRotation = transform.parent != null ? transform.parent.rotation : Quaternion.identity;

        Vector3 baseDir = parentRotation * Vector3.left;

        Vector3 minDir = Quaternion.Euler(0f, 0f, minRotation) * baseDir;
        Vector3 maxDir = Quaternion.Euler(0f, 0f, maxRotation) * baseDir;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + minDir * gizmoRadius);
        Gizmos.DrawLine(origin, origin + maxDir * gizmoRadius);

        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.35f);
        int segments = 24;
        Vector3 prevPoint = origin + minDir * gizmoRadius;

        for (int i = 1; i <= segments; i++) {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(minRotation, maxRotation, t);
            Vector3 dir = Quaternion.Euler(0f, 0f, angle) * baseDir;
            Vector3 nextPoint = origin + dir * gizmoRadius;

            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        if (centerPoint != null) {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + -centerPoint.right * (gizmoRadius * 1.15f));
        }
    }
}
