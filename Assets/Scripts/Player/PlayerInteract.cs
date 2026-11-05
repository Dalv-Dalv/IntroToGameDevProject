using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    private InputAction moveAction;
    private InputAction interactAction;
    private InputAction backAction;

    [Header("References")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Detection Settings")]
    [SerializeField] private float interactRadius = 1.5f;
    [SerializeField] private LayerMask interactableLayer;

    private SpaceshipModule currentModule;

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();

        moveAction = playerInput.actions["Move"];
        interactAction = playerInput.actions["Interact"];
        backAction = playerInput.actions.FindAction("Back");
    }

    private void OnEnable()
    {
        interactAction.Enable();
        interactAction.performed += OnInteractPerformed;

        if (backAction != null)
        {
            backAction.Enable();
            backAction.performed += OnBackPerformed;
        }
    }

    private void OnDisable()
    {
        ReleaseCurrentModule();

        interactAction.performed -= OnInteractPerformed;
        if (backAction != null)
        {
            backAction.performed -= OnBackPerformed;
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (currentModule != null)
        {
            ReleaseCurrentModule();
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactableLayer);

        SpaceshipModule closestModule = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<SpaceshipModule>(out var module) && module.enabled && !module.IsOccupied)
            {
                float distance = Vector2.Distance(transform.position, hit.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestModule = module;
                }
            }
        }

        if (closestModule != null && closestModule.TryOccupy(this))
        {
            currentModule = closestModule;
            if (playerMovement != null) playerMovement.SetUsingModule(true);
        }
    }

    private void OnBackPerformed(InputAction.CallbackContext ctx)
    {
        ReleaseCurrentModule();
    }

    public void ReleaseCurrentModule()
    {
        if (currentModule != null)
        {
            currentModule.Release();
            currentModule = null;

            if (playerMovement != null) playerMovement.SetUsingModule(false);
        }
    }

    private void Update()
    {
        if (currentModule != null)
        {
            if (Vector2.Distance(transform.position, currentModule.transform.position) > interactRadius * 1.5f)
            {
                ReleaseCurrentModule();
                return;
            }

            Vector2 input = moveAction.ReadValue<Vector2>();
            currentModule.SendInput(input);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}