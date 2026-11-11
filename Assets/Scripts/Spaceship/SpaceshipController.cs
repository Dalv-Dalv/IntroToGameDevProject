using UnityEngine;

public class SpaceshipController : MonoBehaviour
{
    [Header("Module References")]
    [SerializeField] protected SpaceshipModule spaceshipController;

    [Tooltip("Optional: Transform where the player is aligned horizontally when operating this module.")]
    [SerializeField] private Transform standPoint;

    public PlayerInteract CurrentOperator { get; private set; }
    public bool IsOccupied => CurrentOperator != null;

  
    public Transform VisualTransform { get; private set; }

    public virtual void Initialize(SpaceshipModule controller, Transform visualTransform)
    {
        spaceshipController = controller;
        VisualTransform = visualTransform;
    }

    protected virtual void Awake()
    {
        if (spaceshipController == null)
        {
            spaceshipController = GetComponentInParent<SpaceshipModule>();
        }
        if (VisualTransform == null)
        {
            VisualTransform = transform;
        }
    }

    public bool TryOccupy(PlayerInteract player)
    {
        if (IsOccupied) return false;

        CurrentOperator = player;

        if (standPoint != null)
        {
            player.transform.position = new Vector3(
                standPoint.position.x,
                player.transform.position.y,
                player.transform.position.z
            );
        }

        OnOperatorEntered(player);
        return true;
    }

    public void Release()
    {
        if (!IsOccupied) return;

        PlayerInteract previousOperator = CurrentOperator;
        CurrentOperator = null;
        OnOperatorExited(previousOperator);
    }

    public virtual void SetMoveInput(Vector2 directionalInput){
        spaceshipController?.SetMoveInput(directionalInput); 
    }   
    public virtual void SetInteractInput(bool interactInput){
        spaceshipController?.SetInteractInput(interactInput);

    }
    protected virtual void OnOperatorEntered(PlayerInteract player) { }
    protected virtual void OnOperatorExited(PlayerInteract player) { 
        spaceshipController?.SetMoveInput(Vector2.zero);
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (spaceshipController == null)
        {
            spaceshipController = GetComponentInParent<SpaceshipModule>();
        }
    }
#endif
}