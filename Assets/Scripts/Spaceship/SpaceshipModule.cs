using UnityEngine;

public abstract class SpaceshipModule : MonoBehaviour {
    protected Vector2 currentMoveInput;
    protected bool currentInteractInput;

    public void SetMoveInput(Vector2 input) {
        currentMoveInput = Vector2.ClampMagnitude(input, 1f);
    }
    public void SetInteractInput(bool input) {
        currentInteractInput = input;
    }
}
