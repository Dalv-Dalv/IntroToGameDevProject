// Lives on the ship root. Pickups hand their resource to it on touch.
// Returns how much was accepted (0 = full / not wanted; the pickup then stays).
public interface IResourceReceiver {
    float Receive(ResourceType type, float amount);
}
