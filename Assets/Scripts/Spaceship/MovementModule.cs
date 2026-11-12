using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovementModule : SpaceshipModule{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 8f;

    private Rigidbody2D rb;

    private void Awake(){
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate(){      
        Vector2 forceNeeded = currentMoveInput * maxSpeed *rb.mass* Time.fixedDeltaTime;
        rb.AddForce(forceNeeded, ForceMode2D.Force);
    }

#if UNITY_EDITOR
    private void OnValidate(){
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (!Application.isPlaying) return;
    }
#endif
}
