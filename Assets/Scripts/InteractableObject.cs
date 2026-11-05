using UnityEngine;

public class InteractableObject : SpaceshipModule
{
    public override void SendInput(Vector2 directionalInput)
    {
        if (spaceshipController != null)
        {
            spaceshipController.SetMoveInput(directionalInput);
        }
    }

    protected override void OnOperatorExited(PlayerInteract player)
    {
        if (spaceshipController != null)
        {
            spaceshipController.SetMoveInput(Vector2.zero);
        }
    }
}