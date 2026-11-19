using UnityEngine;

// Creates one map player for every player in the roster
public class MapPlayerSpawner : MonoBehaviour {
    [SerializeField] private GameObject mapPlayerPrefab;

    void Start() {
        PlayerRoster.SpawnAll(mapPlayerPrefab);
    }
}
