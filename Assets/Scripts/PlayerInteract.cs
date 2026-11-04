using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class PlayerInteract : MonoBehaviour
{
    private InputAction moveAction;
    private InputAction interactAction;
    private InputAction jumpAction; 
    private InputAction backAction;
    
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private MonoBehaviour playerMovementScript; // Trage scriptul tău de mișcare (ex: PlayerMovement) aici

    [Header("Detection Settings")]
    [SerializeField] private float interactRadius = 1.5f;
    [SerializeField] private LayerMask interactableLayer;

    private InteractableObject currentInteractable;

    private void Awake()
    {
        moveAction = playerInput.actions["Move"];
        interactAction = playerInput.actions["Interact"];
        jumpAction = playerInput.actions["Jump"];
        backAction = playerInput.actions["Back"];
    }
    void Start()
    {
        backAction.Enable();
        backAction.performed += _ => OnBack();
        interactAction.Enable();
        interactAction.performed += _ => OnInteract();
    }

    void OnInteract()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactableLayer);

        InteractableObject closestInteractable = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<InteractableObject>(out var interactable))
            {
                float distance = Vector2.Distance(transform.position, hit.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }
        Debug.Log(closestInteractable);
        if (closestInteractable != null)
        {
            currentInteractable = closestInteractable;
            StartControlling(currentInteractable);
             
        }
    }
    void OnBack()
    {
        if (currentInteractable != null)
        {
            FinishControlling();
        }
    }
    private void StartControlling(InteractableObject target)
    {
        currentInteractable = target;
        currentInteractable.Interact();

        // Oprește mișcarea normală a jucătorului
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }
    }
    public void FinishControlling()
    {
        if (currentInteractable != null)
        {
            currentInteractable.StopControl();
            currentInteractable = null;

            // Redă controlul jucătorului
            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = true;
            }
        }
    }
    void Update()
    {
        if (currentInteractable != null)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            currentInteractable.Control(moveInput);
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}

