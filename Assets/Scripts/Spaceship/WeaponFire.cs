using System;
using UnityEngine;

public class WeaponFire : SpaceshipModule {

    [SerializeField] private float cooldown = 2.0f;
    [SerializeField] private float force = 0.0f;
    [SerializeField] private float rotationSpeed = 10.0f;
    [SerializeField] private Transform CenterPoint;

    [SerializeField] private float minRotation = -50.0f;
    [SerializeField] private float maxRotation = 50.0f;
    [SerializeField] private float gizmoRadius = 2.0f;
    private float currentTime = 0.0f;
    private float currentRotation = 0.0f;
    [SerializeField] GameObject bullet;
    void Start() {
        float initialZ = transform.localEulerAngles.z;
        if (initialZ > 180f) initialZ -= 360f;
        currentRotation = Mathf.Clamp(initialZ, minRotation, maxRotation);
        transform.localRotation = Quaternion.Euler(0f, 0f, currentRotation);
    }

    // Update is called once per frame
    void Update() {
        currentTime += Time.deltaTime;
        if (currentTime > cooldown && currentInteractInput == true) {
            GameObject spawnedBullet = Instantiate(bullet, CenterPoint.position, Quaternion.identity);
            Rigidbody2D rb = spawnedBullet.GetComponent<Rigidbody2D>();
            rb.AddForce(-CenterPoint.transform.right * force, ForceMode2D.Impulse);
            currentTime = 0;
        }
        if (currentMoveInput != Vector2.zero) {
            currentRotation += -currentMoveInput.y * Time.deltaTime * rotationSpeed;
            currentRotation = Mathf.Clamp(currentRotation, minRotation, maxRotation);
            transform.localRotation = Quaternion.Euler(0f, 0f, currentRotation);
        }

    }
    private void OnDrawGizmosSelected() {
        Vector3 origin = CenterPoint != null ? CenterPoint.position : transform.position;

        // Luăm rotația părintelui pentru ca gizmo-ul să se rotească odată cu nava
        Quaternion parentRotation = transform.parent != null ? transform.parent.rotation : Quaternion.identity;

        // Direcția de bază a armei (-right, așa cum tragi în Update)
        Vector3 baseDir = parentRotation * Vector3.left;

        Vector3 minDir = Quaternion.Euler(0f, 0f, minRotation) * baseDir;
        Vector3 maxDir = Quaternion.Euler(0f, 0f, maxRotation) * baseDir;

        // Liniile de limită (min și max)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + minDir * gizmoRadius);
        Gizmos.DrawLine(origin, origin + maxDir * gizmoRadius);

        // Arcul de cerc între minRotation și maxRotation
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

        // Direcția curentă de tragere (roșu)
        if (CenterPoint != null) {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + (-CenterPoint.right) * (gizmoRadius * 1.15f));
        }
    }
}
