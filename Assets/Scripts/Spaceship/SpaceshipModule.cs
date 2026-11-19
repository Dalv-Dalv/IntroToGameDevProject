using UnityEngine;

public abstract class SpaceshipModule : MonoBehaviour {
    protected bool currentInteractInput;
    protected Vector2 currentMoveInput;

    public void SetMoveInput(Vector2 input) {
        currentMoveInput = Vector2.ClampMagnitude(input, 1f);
    }
    public void SetInteractInput(bool input) {
        currentInteractInput = input;
    }
}
