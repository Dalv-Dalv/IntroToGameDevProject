using UnityEngine;

// One hit. sourcePos = where the hit came from (null = no knockback), source = who dealt it (may be null)
public struct DamageInfo {
    public float amount;
    public DamageType type;
    public Vector2? sourcePos;
    public GameObject source;

    public DamageInfo(float amount, DamageType type = DamageType.Generic, Vector2? sourcePos = null, GameObject source = null) {
        this.amount = amount;
        this.type = type;
        this.sourcePos = sourcePos;
        this.source = source;
    }
}
