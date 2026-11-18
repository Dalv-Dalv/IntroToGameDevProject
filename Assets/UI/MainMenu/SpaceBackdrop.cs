using UnityEngine;
using UnityEngine.UIElements;

/// Draws the menu's animated background: drifting stars, a planet, and the crew's ship.
internal static class SpaceBackdrop {
    private const int StarCount = 170;

    private static readonly Color PlanetRim = new(0.48f, 0.38f, 0.86f);
    private static readonly Color PlanetBody = new(0.18f, 0.13f, 0.38f);
    private static readonly Color Crater = new(0.14f, 0.10f, 0.31f);
    private static readonly Color Hull = new(0.91f, 0.89f, 1f);
    private static readonly Color HullShade = new(0.66f, 0.62f, 0.86f);
    private static readonly Color Fin = new(1f, 0.42f, 0.42f);
    private static readonly Color Window = new(0.30f, 0.79f, 0.94f);
    private static readonly Color Flame = new(1f, 0.81f, 0.35f);

    private static readonly Vector3[] Stars = CreateStars();

    private static Vector3[] CreateStars() {
        var random = new System.Random(7);
        var stars = new Vector3[StarCount];
        // x, y are normalized positions; z is depth (0 = far, 1 = near).
        for (int i = 0; i < StarCount; i++)
            stars[i] = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
        return stars;
    }

    public static void Draw(MeshGenerationContext context) {
        var rect = context.visualElement.contentRect;
        if (rect.width <= 0 || rect.height <= 0) return;
        var painter = context.painter2D;
        float t = Time.unscaledTime;
        float h = rect.height;

        void Disc(Vector2 center, float radius, Color color) {
            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(center, radius, 0, 360);
            painter.ClosePath();
            painter.Fill();
        }

        for (int i = 0; i < Stars.Length; i++) {
            var star = Stars[i];
            float x = Mathf.Repeat(star.x - t * (0.002f + 0.01f * star.z), 1f) * rect.width;
            float twinkle = 0.75f + 0.25f * Mathf.Sin(t * (1f + star.z * 2f) + i);
            Disc(new Vector2(x, star.y * h), 0.6f + 1.4f * star.z * star.z,
                new Color(1f, 1f, 1f, (0.25f + 0.6f * star.z) * twinkle));
        }

        // Planet: a lit disc with a slightly offset body on top leaves a thin rim on the upper left.
        var planet = new Vector2(rect.width * 0.86f, h * 1.12f);
        float radius = h * 0.62f;
        for (int i = 3; i >= 1; i--) Disc(planet, radius + i * 9f, new Color(PlanetRim.r, PlanetRim.g, PlanetRim.b, 0.05f));
        Disc(planet, radius, PlanetRim);
        Disc(planet + new Vector2(radius, radius) * 0.025f, radius, PlanetBody);
        Disc(planet + new Vector2(-0.42f, -0.62f) * radius, radius * 0.07f, Crater);
        Disc(planet + new Vector2(-0.18f, -0.80f) * radius, radius * 0.045f, Crater);
        Disc(planet + new Vector2(-0.66f, -0.35f) * radius, radius * 0.05f, Crater);

        DrawShip(painter, new Vector2(rect.width * 0.68f, h * (0.36f + 0.01f * Mathf.Sin(t * 1.3f))), h * 0.075f, t);
    }

    private static void DrawShip(Painter2D painter, Vector2 origin, float size, float t) {
        float angle = (-14f + 2f * Mathf.Sin(t * 0.9f)) * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
        Vector2 P(float x, float y) => origin + new Vector2(x * cos - y * sin, x * sin + y * cos) * size;

        void Shape(Color color, params Vector2[] points) {
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++) painter.LineTo(points[i]);
            painter.ClosePath();
            painter.Fill();
        }

        float flicker = 0.38f + 0.1f * Mathf.Sin(t * 31f) + 0.05f * Mathf.Sin(t * 17f);
        Shape(Flame, P(-0.9f, -0.17f), P(-0.9f - flicker * 2.2f, 0), P(-0.9f, 0.17f));
        Shape(new Color(1f, 0.96f, 0.82f), P(-0.9f, -0.08f), P(-0.9f - flicker * 1.1f, 0), P(-0.9f, 0.08f));

        Shape(Fin, P(-0.3f, -0.3f), P(-0.85f, -0.85f), P(-1.05f, -0.85f), P(-0.8f, -0.3f));
        Shape(Fin, P(-0.3f, 0.3f), P(-0.85f, 0.85f), P(-1.05f, 0.85f), P(-0.8f, 0.3f));
        Shape(Hull, P(1.3f, 0), P(0.8f, -0.3f), P(-0.7f, -0.36f), P(-0.92f, -0.2f),
            P(-0.92f, 0.2f), P(-0.7f, 0.36f), P(0.8f, 0.3f));
        Shape(HullShade, P(1.3f, 0), P(0.8f, 0.3f), P(-0.7f, 0.36f), P(-0.92f, 0.2f), P(-0.92f, 0.06f));
        Shape(Fin, P(1.3f, 0), P(1.0f, -0.17f), P(1.0f, 0.17f));

        painter.fillColor = Window;
        painter.BeginPath();
        painter.Arc(P(0.45f, -0.05f), size * 0.15f, 0, 360);
        painter.ClosePath();
        painter.Fill();
    }
}
