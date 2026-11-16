using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR || DEBUG
public class LevelDebugKeys : MonoBehaviour {
    void Update() {
        var kb = Keyboard.current;
        if (kb == null || LevelManager.Instance == null) return;
        if (kb.f9Key.wasPressedThisFrame) LevelManager.Instance.Complete();
        if (kb.f10Key.wasPressedThisFrame) LevelManager.Instance.Fail();
    }
}
#endif
