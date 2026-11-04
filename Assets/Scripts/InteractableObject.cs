using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private Rigidbody2D rb;

    private PlayerInteract posseser;
    public virtual void Interact()
    {
       
    }

    public virtual void Control(Vector2 direction)
    {
        if (rb != null)
        {
            rb.AddForce(direction * speed, ForceMode2D.Force);
        }
    }

    public virtual void StopControl()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
