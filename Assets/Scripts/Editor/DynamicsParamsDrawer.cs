using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DynamicsParams))]
public class DynamicsParamsDrawer : PropertyDrawer {
    const float GraphHeight = 120f;
    const float Padding = 10f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        float lineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        return lineSpace * 4 + Padding + GraphHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty fProp = property.FindPropertyRelative("f");
        SerializedProperty zProp = property.FindPropertyRelative("z");
        SerializedProperty rProp = property.FindPropertyRelative("r");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        Rect currentRect = new Rect(position.x, position.y, position.width, lineHeight);
        EditorGUI.LabelField(currentRect, label, EditorStyles.boldLabel);

        currentRect.y += lineHeight + spacing;
        EditorGUI.PropertyField(currentRect, fProp);

        currentRect.y += lineHeight + spacing;
        EditorGUI.PropertyField(currentRect, zProp);

        currentRect.y += lineHeight + spacing;
        EditorGUI.PropertyField(currentRect, rProp);

        currentRect.y += lineHeight + Padding;
        currentRect.height = GraphHeight;
        DrawDynamicsGraph(currentRect, fProp.floatValue, zProp.floatValue, rProp.floatValue);

        EditorGUI.EndProperty();
    }

    void DrawDynamicsGraph(Rect rect, float f, float z, float r) {
        f = Mathf.Max(0.01f, f);

        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

        float baselineY = rect.y + rect.height * 0.8f;
        float targetY = rect.y + rect.height * 0.3f;

        Handles.color = new Color(1, 1, 1, 0.2f);
        Handles.DrawLine(new Vector3(rect.x, baselineY), new Vector3(rect.x + rect.width, baselineY));
        Handles.DrawLine(new Vector3(rect.x, targetY), new Vector3(rect.x + rect.width, targetY));

        const int steps = 150;
        const float dt = 1.5f / steps;
        var points = new Vector3[steps];
        float y = 0, yd = 0;

        float k1 = z / (Mathf.PI * f);
        float k2 = 1f / (2f * Mathf.PI * f * (2f * Mathf.PI * f));
        float k3 = r * z / (2f * Mathf.PI * f);

        float previousX = 0f;

        for (int i = 0; i < steps; i++) {
            float k2Stable = Mathf.Max(k2, 1.1f * (dt * dt / 4f + dt * k1 / 2f));

            float currentX = 1f;
            float xd = (currentX - previousX) / dt;
            previousX = currentX;

            y += dt * yd;
            yd += dt * (currentX + k3 * xd - y - k1 * yd) / k2Stable;

            float px = rect.x + i / (float)(steps - 1) * rect.width;
            float py = Mathf.LerpUnclamped(baselineY, targetY, y);
            py = Mathf.Clamp(py, rect.y, rect.y + rect.height);

            points[i] = new Vector3(px, py, 0);
        }

        Handles.color = Color.cyan;
        Handles.DrawAAPolyLine(2.5f, points);
    }
}
