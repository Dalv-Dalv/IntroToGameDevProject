using System;
using UnityEngine;

// Fires Projectile prefabs. Behaviors call TryFire(direction); it respects the cooldown and won't fire while the
// level isn't Playing. Projectiles ignore this launcher's owner (its Rigidbody2D's GameObject). Generic: enemies, turrets.
public class ProjectileLauncher : MonoBehaviour {
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform muzzle;            // spawn point; this transform if empty
    [SerializeField] private float spawnOffset = 0.7f;    // spawn this far along the shot direction (clear of the body)
    [SerializeField] private float cooldown = 1.2f;
    [SerializeField] private float spreadDegrees = 0f;    // random +- spread per shot

    public event Action<Projectile> OnFired;

    public bool IsReady => Time.time >= nextFireTime;
    public float Cooldown => cooldown;
    public Vector2 MuzzlePosition => (muzzle != null ? muzzle : transform).position;

    private GameObject owner;
    private float nextFireTime;

    void Awake() {
        var body = GetComponentInParent<Rigidbody2D>();
        owner = body != null ? body.gameObject : gameObject;
    }

    void OnValidate() {
        spawnOffset = Mathf.Max(0f, spawnOffset);
        cooldown = Mathf.Max(0f, cooldown);
        spreadDegrees = Mathf.Clamp(spreadDegrees, 0f, 180f);
    }

    public bool TryFire(Vector2 direction) {
        if (projectilePrefab == null || !IsReady || !LevelGate.IsPlaying) return false;
        if (direction.sqrMagnitude < 0.0001f) return false;

        Vector2 dir = direction.normalized;
        if (spreadDegrees > 0f) dir = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-spreadDegrees, spreadDegrees)) * dir;

        Vector2 pos = MuzzlePosition + dir * spawnOffset;
        var shot = Instantiate(projectilePrefab, pos, Quaternion.identity);
        shot.Launch(dir, owner);

        nextFireTime = Time.time + cooldown;
        OnFired?.Invoke(shot);
        return true;
    }
}
