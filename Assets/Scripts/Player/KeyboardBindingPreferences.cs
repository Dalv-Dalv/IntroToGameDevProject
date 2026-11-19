using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class KeyboardBindingPreferences {
    private const string PrefsKey = "KeyboardBindingOverrides.v1";

    public static void Apply(InputActionAsset actions) {
        try {
            actions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PrefsKey));
        } catch (ArgumentException) {
            actions.RemoveAllBindingOverrides();
            Debug.LogWarning("Saved keyboard bindings could not be read; using defaults.");
        }
    }

    public static void Save(InputActionAsset actions) {
        PlayerPrefs.SetString(PrefsKey, actions.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
    }
}
