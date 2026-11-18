using UnityEngine;

// Placeholder sprites made at runtime, both are 1 unit wide and white so they can be tinted
public static class MapSprites {
    private static Sprite circle;
    private static Sprite square;

    public static Sprite Circle() {
        if (circle == null) circle = Make(true);
        return circle;
    }

    public static Sprite Square() {
        if (square == null) square = Make(false);
        return square;
    }

    private static Sprite Make(bool isCircle) {
        int size = 64;
        var texture = new Texture2D(size, size);
        var center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float alpha = 1f;
                if (isCircle) {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    alpha = Mathf.Clamp01(size / 2f - distance);
                }
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
