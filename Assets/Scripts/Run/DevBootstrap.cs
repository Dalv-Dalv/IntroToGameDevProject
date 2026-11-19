#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Editor only: lets you press Play directly in the map or level scene
// It makes the run and the keyboard player that the join screen would have made
public static class DevBootstrap {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init() {
        string scene = SceneManager.GetActiveScene().name;
        if (scene != SceneNames.Map && scene != SceneNames.Gameplay) return;

        RunManager.CreateRun();
        PlayerRoster.Add("KeyboardP1", Keyboard.current);

        // The level scene needs a level to load
        if (scene == SceneNames.Gameplay) RunManager.State.PendingNode = RunManager.State.map.Row(0)[0];
    }
}
#endif
