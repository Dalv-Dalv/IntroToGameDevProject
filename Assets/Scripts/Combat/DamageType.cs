using System;

// Damage categories (not enemy types). Additive only: never renumber or remove values.
// Damageable.acceptedTypes is a mask of these, so an object can ignore some categories.
[Flags]
public enum DamageType {
    None = 0,
    Generic = 1 << 0,    // debug / scripted
    Projectile = 1 << 1, // anything fired
    Contact = 1 << 2,    // touching / ramming
    Module = 1 << 3,     // special ship-module weapons (not plain turret bullets)
    Explosion = 1 << 4,  // area damage
}
