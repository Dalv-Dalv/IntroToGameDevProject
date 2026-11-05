using UnityEngine;

public abstract class SpaceshipModule : MonoBehaviour
{
    [Header("Module References")]
    [SerializeField] protected SpaceshipController spaceshipController;

    [Tooltip("Optional: Transform where the player is aligned horizontally when operating this module.")]
    [SerializeField] private Transform standPoint;

    public PlayerInteract CurrentOperator { get; private set; }
    public bool IsOccupied => CurrentOperator != null;

  
    public Transform VisualTransform { get; private set; }

    public virtual void Initialize(SpaceshipController controller, Transform visualTransform)
    {
        spaceshipController = controller;
        VisualTransform = visualTransform;
    }

    protected virtual void Awake()
    {
        if (spaceshipController == null)
        {
            spaceshipController = GetComponentInParent<SpaceshipController>();
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

    public abstract void SendInput(Vector2 directionalInput);
    protected virtual void OnOperatorEntered(PlayerInteract player) { }
    protected virtual void OnOperatorExited(PlayerInteract player) { }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (spaceshipController == null)
        {
            spaceshipController = GetComponentInParent<SpaceshipController>();
        }
    }
#endif
}