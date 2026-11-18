using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GoalZone : MonoBehaviour {
    void Awake() {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other) {
        var level = LevelManager.Instance;
        if (other.attachedRigidbody == level.Ship) level.Complete();
    }
}
