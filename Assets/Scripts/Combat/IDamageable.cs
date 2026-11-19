// Anything that can be hit. Dealers find it with DamageUtil.TryGetDamageable(collider, out target)
public interface IDamageable {
    void DealDamage(DamageInfo hit);
}
