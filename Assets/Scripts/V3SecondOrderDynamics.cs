using UnityEngine;

public class V3SecondOrderDynamics {
    private Vector3 xp;
    private Vector3 y, yd;
    private float k1, k2, k3;

    public V3SecondOrderDynamics(float f, float z, float r, Vector3 x0) {
        ChangeParameters(f, z, r);
        xp = x0;
        y = x0;
        yd = Vector3.zero;
    }

    public void ChangeParameters(float f, float z, float r) {
        k1 = z / (Mathf.PI * f);
        k2 = 1f / ((2f * Mathf.PI * f) * (2f * Mathf.PI * f));
        k3 = r * z / (2f * Mathf.PI * f);
    }

    public Vector3 Update(float T, Vector3 x, Vector3 xd) {
        float k2Stable = Mathf.Max(k2, 1.1f * (T * T / 4f + T * k1 / 2f));
        y += T * yd;
        yd += T * (x + k3 * xd - y - k1 * yd) / k2Stable;
        return y;
    }
}