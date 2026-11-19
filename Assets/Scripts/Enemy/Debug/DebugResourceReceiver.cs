using System.Collections.Generic;
using UnityEngine;

// Test only: stands in for the ship's Light/resources until a teammate's receiver exists.
// Put it on the Spaceship instance in EnemyTestScene (scene override). Caps each resource, logs and shows totals.
public class DebugResourceReceiver : MonoBehaviour, IResourceReceiver {
    [SerializeField] private float capacity = 10f; // per resource, 0 = unlimited
    [SerializeField] private bool showOnScreen = true;

    private readonly Dictionary<ResourceType, float> totals = new Dictionary<ResourceType, float>();

    public float Capacity => capacity;

    public float Get(ResourceType type) {
        return totals.TryGetValue(type, out float v) ? v : 0f;
    }

    public float Receive(ResourceType type, float amount) {
        if (amount <= 0f) return 0f;

        float current = Get(type);
        float room = capacity > 0f ? Mathf.Max(0f, capacity - current) : amount;
        float accepted = Mathf.Min(amount, room);
        if (accepted <= 0f) return 0f;

        totals[type] = current + accepted;
        Debug.Log($"[DebugResourceReceiver] +{accepted} {type} -> {totals[type]}{(capacity > 0f ? "/" + capacity : "")}");
        return accepted;
    }

    void OnGUI() {
        if (!showOnScreen) return;
        string cap = capacity > 0f ? " / " + capacity : "";
        GUI.Label(new Rect(10, 10, 300, 24), $"{ResourceType.Light}: {Get(ResourceType.Light)}{cap}");
    }
}
