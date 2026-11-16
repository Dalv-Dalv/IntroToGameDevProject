using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GoalZone : MonoBehaviour {
    void Reset() {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Awake() {
        GetComponent<Collider2D>().isTrigger = true;

        // Placeholder: green square if the sprite renderer has no sprite
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite == null) {
            var tex = Texture2D.whiteTexture;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            sr.color = new Color(0.2f, 1f, 0.3f, 0.5f);
        }
    }

    void OnTriggerEnter2D(Collider2D other) {
        var rb = other.attachedRigidbody;
        if (rb != null && rb.GetComponent<PlayerAndSpaceshipSync>() != null) {
            LevelManager.Instance?.Complete();
        }
    }
}
