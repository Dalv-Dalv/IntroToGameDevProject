using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour {
    [Header("References")] [SerializeField]
    PlayerInput playerInput;

    [SerializeField] PlayerMovement playerMovement;

    [Header("Detection Settings")] [SerializeField]
    float interactRadius = 1.5f;

    [SerializeField] LayerMask interactableLayer;
    InputAction backAction;

    SpaceshipController currentModule;
    InputAction interactAction;
    InputAction moveAction;

    void Awake() {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();

        moveAction = playerInput.actions["Move"];
        interactAction = playerInput.actions["Interact"];
        backAction = playerInput.actions["Back"];
    }

    void Update() {
        if (currentModule != null) {
            if (Vector2.Distance(transform.position, currentModule.transform.position) > interactRadius * 1.5f) {
                ReleaseCurrentModule();
                return;
            }

            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            bool interactInput = interactAction.IsPressed();
            currentModule.SetMoveInput(moveInput);
            currentModule.SetInteractInput(interactInput);
        }
    }

    void OnEnable() {
        interactAction.Enable();
        interactAction.performed += OnInteractPerformed;

        if (backAction != null) {
            backAction.Enable();
            backAction.performed += OnBackPerformed;
        }
    }

    void OnDisable() {
        ReleaseCurrentModule();

        interactAction.performed -= OnInteractPerformed;
        if (backAction != null) {
            backAction.performed -= OnBackPerformed;
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }

    void OnInteractPerformed(InputAction.CallbackContext ctx) {
        if (currentModule != null) {
            return;
        }

        var hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactableLayer);

        SpaceshipController closestModule = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider2D hit in hits) {
            if (hit.TryGetComponent(out SpaceshipController module) && module.enabled && !module.IsOccupied) {
                float distance = Vector2.Distance(transform.position, hit.transform.position);
                if (distance < minDistance) {
                    minDistance = distance;
                    closestModule = module;
                }
            }
        }

        if (closestModule != null && closestModule.TryOccupy(this)) {
            currentModule = closestModule;
            if (playerMovement != null) playerMovement.SetUsingModule(true);
        }
    }

    void OnBackPerformed(InputAction.CallbackContext ctx) {
        ReleaseCurrentModule();
    }

    public void ReleaseCurrentModule() {
        if (currentModule != null) {
            currentModule.Release();
            currentModule = null;

            if (playerMovement != null) playerMovement.SetUsingModule(false);
        }
    }
}
