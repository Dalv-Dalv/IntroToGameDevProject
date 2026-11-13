using UnityEngine;

public class V2SecondOrderDynamics {
    private Vector2 xp;
    private Vector2 y, yd;
    private float k1, k2, k3;

    public V2SecondOrderDynamics(float f, float z, float r, Vector2 x0) {
        ChangeParameters(f, z, r);

        xp = x0;
        y = x0;
        yd = Vector2.zero;
    }

    public void ChangeParameters(float f, float z, float r) {
        k1 = z / (Mathf.PI * f);
        k2 = 1 / (2 * Mathf.PI * f * (2 * Mathf.PI * f));
        k3 = r * z / (2 * Mathf.PI * f);
    }

    public Vector2 Update(float dt, Vector2 x, Vector2 xd) {
        var k2Stable = Mathf.Max(k2, 1.1f * (dt * dt / 4 + dt * k1 / 2));
        y += dt * yd;
        yd += dt * (x + k3 * xd - y - k1 * yd) / k2Stable;
        return y;
    }
}
