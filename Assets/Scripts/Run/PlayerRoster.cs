using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public struct RosterEntry {
    public string controlScheme;
    public InputDevice device;
}

// Remembers which device and control scheme pairs have joined, it is static so it survives scene loads
// Player objects are not kept, they are respawned from these entries in each scene
public static class PlayerRoster {
    private static readonly List<RosterEntry> entries = new();

    public static IReadOnlyList<RosterEntry> Entries => entries;
    public static int Count => entries.Count;

    public static void Add(string scheme, InputDevice device) {
        if (Contains(scheme, device)) return;
        entries.Add(new RosterEntry { controlScheme = scheme, device = device });
    }

    public static bool Contains(string scheme, InputDevice device) {
        foreach (var e in entries) {
            if (e.controlScheme == scheme && e.device == device) return true;
        }
        return false;
    }

    public static bool UsesDevice(InputDevice device) {
        foreach (var e in entries) {
            if (e.device == device) return true;
        }
        return false;
    }

    public static bool UsesScheme(string scheme) {
        foreach (var e in entries) {
            if (e.controlScheme == scheme) return true;
        }
        return false;
    }

    // Statics are not reset when domain reload is disabled for play mode, so clear them on start
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear() {
        entries.Clear();
    }
}
