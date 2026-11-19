using System.Collections.Generic;
using UnityEngine;

// A dashed line between two nodes
public class MapEdge : MonoBehaviour {
    public MapNode From { get; private set; }

    private readonly List<SpriteRenderer> dashes = new();
    private Color color;

    public void Init(MapNode from, Vector3 start, Vector3 end, float dashSpacing, Color dashColor) {
        From = from;
        color = dashColor;

        Vector3 direction = end - start;
        int count = Mathf.Max(1, Mathf.FloorToInt(direction.magnitude / dashSpacing));
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        for (int i = 0; i < count; i++) {
            var dash = new GameObject("Dash").AddComponent<SpriteRenderer>();
            dash.transform.SetParent(transform);
            dash.transform.position = start + direction * ((i + 0.5f) / count);
            dash.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            dash.transform.localScale = new Vector3(0.18f, 0.06f, 1f);
            dash.sprite = MapSprites.Square();
            dash.sortingOrder = 0;
            dashes.Add(dash);
        }
        SetLive(true);
    }

    // Edges that can't be reached anymore are faded
    public void SetLive(bool live) {
        var c = color;
        c.a = live ? 1f : 0.2f;
        foreach (var dash in dashes) dash.color = c;
    }
}
