using UnityEngine;

// Tints the sprite(s) briefly when the Damageable is hit; blocked hits get their own tint. Works on anything.
[RequireComponent(typeof(Damageable))]
public class DamageFlash : MonoBehaviour {
    [SerializeField] private SpriteRenderer[] renderers; // all child sprites if empty
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;

    [Header("Blocked hits (wrong damage type / invulnerable)")]
    [SerializeField] private bool flashOnBlocked = true;
    [SerializeField] private Color blockedColor = new Color(0.4f, 0.8f, 1f);

    private Damageable damageable;
    private Color[] baseColors;
    private float flashUntil = -1f;

    void Awake() {
        damageable = GetComponent<Damageable>();
        if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>(true);

        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) {
            if (renderers[i] != null) baseColors[i] = renderers[i].color;
        }
    }

    void OnEnable() {
        damageable.OnDamaged += HandleDamaged;
        damageable.OnDamageBlocked += HandleBlocked;
    }

    void OnDisable() {
        damageable.OnDamaged -= HandleDamaged;
        damageable.OnDamageBlocked -= HandleBlocked;
        EndFlash();
    }

    void OnValidate() {
        flashDuration = Mathf.Max(0f, flashDuration);
    }

    void Update() {
        if (flashUntil >= 0f && Time.time >= flashUntil) EndFlash();
    }

    private void HandleDamaged(DamageInfo hit) {
        Flash(flashColor);
    }

    private void HandleBlocked(DamageInfo hit) {
        if (flashOnBlocked) Flash(blockedColor);
    }

    private void Flash(Color color) {
        if (flashDuration <= 0f) return;
        foreach (var r in renderers) {
            if (r != null) r.color = color;
        }
        flashUntil = Time.time + flashDuration; // repeated hits just extend it
    }

    private void EndFlash() {
        flashUntil = -1f;
        if (baseColors == null) return;
        for (int i = 0; i < renderers.Length; i++) {
            if (renderers[i] != null) renderers[i].color = baseColors[i];
        }
    }
}
