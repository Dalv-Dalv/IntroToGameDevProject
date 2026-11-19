using UnityEngine;

// Read-only shortcut to "is the level running, and where is the ship". Holds no state.
// Anything that moves, attacks, spawns or collects should halt while IsPlaying is false.
public static class LevelGate {
    public static bool IsPlaying {
        get {
            var level = LevelManager.Instance;
            return level != null && level.State == LevelManager.LevelState.Playing;
        }
    }

    // Null unless the level is Playing (the ship stops being simulated when the level ends)
    public static Rigidbody2D Ship => IsPlaying ? LevelManager.Instance.Ship : null;
}
