using UnityEngine;
using UnityEngine.InputSystem;

// Editor and development builds only: F9 wins the level, F10 loses it
public class LevelDebugKeys : MonoBehaviour {
    void Update() {
        var kb = Keyboard.current;
        if (!Debug.isDebugBuild || kb == null) return;

        if (kb.f9Key.wasPressedThisFrame) LevelManager.Instance.Complete();
        if (kb.f10Key.wasPressedThisFrame) LevelManager.Instance.Fail();
    }
}
