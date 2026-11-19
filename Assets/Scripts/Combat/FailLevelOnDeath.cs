using UnityEngine;

// Fails the level when this Damageable dies (meant for the ship). Does nothing without a LevelManager
[RequireComponent(typeof(Damageable))]
public class FailLevelOnDeath : MonoBehaviour {
    private Damageable damageable;

    void Awake() {
        damageable = GetComponent<Damageable>();
    }

    void OnEnable() {
        damageable.OnDied += HandleDied;
    }

    void OnDisable() {
        damageable.OnDied -= HandleDied;
    }

    private void HandleDied() {
        var level = LevelManager.Instance;
        if (level != null) level.Fail();
    }
}
