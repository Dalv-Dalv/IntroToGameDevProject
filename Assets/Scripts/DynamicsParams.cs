using UnityEngine;

[System.Serializable]
public struct DynamicsParams {
    
    [Tooltip("Frequency")]
    [Range(0.001f, 12f)]
    public float f;
    
    [Tooltip("Damping")]
    [Range(0.001f, 3f)]
    public float z;
    
    [Tooltip("Initial response")]
    [Range(-5f, 5f)]
    public float r;

    public DynamicsParams(float f = 2f, float z = 0.5f, float r = 2f) {
        this.f = f;
        this.z = z;
        this.r = r;
    }
}