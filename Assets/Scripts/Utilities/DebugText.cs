using UnityEngine;

// Big placeholder text over the whole screen, for results and prompts
public static class DebugText {
    public static void Draw(string text, int size, Color color, TextAnchor anchor) {
        var style = new GUIStyle(GUI.skin.label) {
            fontSize = size,
            alignment = anchor,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(20, 20, 40, 40)
        };
        style.normal.textColor = color;
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), text, style);
    }
}
