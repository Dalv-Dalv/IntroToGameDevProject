using UnityEngine;

// Anything that can be hit. sourcePos is optional, pass it when the hit should knock the target back
public interface IDamageable {
    void DealDamage(float amount, Vector2? sourcePos = null);
}
