using System.Collections.Generic;
using UnityEngine;

public class PlayerAndSpaceshipSync : MonoBehaviour {
    static int shipInstanceCounter;

    [SerializeField] Transform interiorRoot;
    [SerializeField] Transform localSpawnPoint;

    readonly List<PlayerProxyPair> activePlayers = new List<PlayerProxyPair>();
    Vector3 interiorLocalOffset;
    Quaternion interiorLocalRotation;
    PlayerJoinManager playerJoinManager;

    Transform staticPhysicsRoom;
    Vector3 staticSpawnPosition;

    void Awake() {
        BuildStaticCollisionRoom();

        playerJoinManager = Object.FindAnyObjectByType<PlayerJoinManager>();
        if (playerJoinManager != null) {
            playerJoinManager.OnPlayerJoined += HandlePlayerJoined;
        }
    }

    void LateUpdate() {
        if (!staticPhysicsRoom) return;

        for (int i = activePlayers.Count - 1; i >= 0; i--) {
            PlayerProxyPair pair = activePlayers[i];
            if (!pair.visualPlayer) {
                if (pair.physicsProxy) Object.Destroy(pair.physicsProxy.gameObject);
                activePlayers.RemoveAt(i);
                continue;
            }

            Vector3 relPos = staticPhysicsRoom.InverseTransformPoint(pair.physicsProxy.position);
            pair.visualPlayer.localPosition = interiorLocalOffset + interiorLocalRotation * relPos;
            pair.visualPlayer.localRotation = interiorLocalRotation;
        }
    }

    void OnDestroy() {
        if (playerJoinManager != null) playerJoinManager.OnPlayerJoined -= HandlePlayerJoined;
        if (staticPhysicsRoom != null) Object.Destroy(staticPhysicsRoom.gameObject);
        foreach (PlayerProxyPair pair in activePlayers) {
            if (pair.physicsProxy != null) Object.Destroy(pair.physicsProxy.gameObject);
        }
    }

    void BuildStaticCollisionRoom() {
        if (interiorRoot == null) return;

        shipInstanceCounter++;
        interiorLocalOffset = transform.InverseTransformPoint(interiorRoot.position);
        interiorLocalRotation = Quaternion.Inverse(transform.rotation) * interiorRoot.rotation;

        Vector3 hiddenPosition = new Vector3(0, - shipInstanceCounter * 500f, 0);

        GameObject staticRoomObj = Object.Instantiate(interiorRoot.gameObject, hiddenPosition, Quaternion.identity);
        staticRoomObj.name = $"{gameObject.name}_PhysicsWallsOnly";
        staticPhysicsRoom = staticRoomObj.transform;

        if (localSpawnPoint != null) {
            Vector3 relSpawn = interiorRoot.InverseTransformPoint(localSpawnPoint.position);
            staticSpawnPosition = staticPhysicsRoom.TransformPoint(relSpawn);
        } else {
            staticSpawnPosition = staticPhysicsRoom.position;
        }
      
        foreach (SpriteRenderer sr in staticRoomObj.GetComponentsInChildren<SpriteRenderer>(true)) Object.Destroy(sr);
        foreach (SpaceshipController module in staticRoomObj.GetComponentsInChildren<SpaceshipController>(true)) Object.Destroy(module);

        foreach (Collider2D col in interiorRoot.GetComponentsInChildren<Collider2D>(true)){
            Object.Destroy(col);
        }
    }

    public void HandlePlayerJoined(GameObject playerObj) {
        if (activePlayers.Exists(p => p.visualPlayer == playerObj.transform)) return;

        GameObject proxyObj = new GameObject($"{playerObj.name}_PhysicsProxy");
        proxyObj.transform.position = staticSpawnPosition;
        proxyObj.layer = playerObj.layer;

        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
        Rigidbody2D proxyRb = proxyObj.AddComponent<Rigidbody2D>();
        if (playerRb) {
            proxyRb.mass = playerRb.mass;
            proxyRb.linearDamping = playerRb.linearDamping;
            proxyRb.angularDamping = playerRb.angularDamping;
            proxyRb.gravityScale = playerRb.gravityScale;
            proxyRb.constraints = playerRb.constraints;
            proxyRb.interpolation = RigidbodyInterpolation2D.Interpolate;

            playerRb.bodyType = RigidbodyType2D.Kinematic;
            playerRb.linearVelocity = Vector2.zero;
        }

        CircleCollider2D playerCircle = playerObj.GetComponent<CircleCollider2D>();
        if (playerCircle) {
            CircleCollider2D proxyCircle = proxyObj.AddComponent<CircleCollider2D>();
            proxyCircle.radius = playerCircle.radius;
            proxyCircle.offset = playerCircle.offset;
            proxyCircle.sharedMaterial = playerCircle.sharedMaterial;
        }

        playerObj.transform.SetParent(transform, false);

        if (playerObj.TryGetComponent(out PlayerMovement movement)) {
            movement.SetPhysicsRigidbody(proxyRb);
        }
        SquashAndStretch squash = playerObj.GetComponentInChildren<SquashAndStretch>();
        if (squash != null) {
            squash.SetPhysicsRigidbody(proxyRb);
        }
        activePlayers.Add(new PlayerProxyPair {
            visualPlayer = playerObj.transform,
            physicsProxy = proxyObj.transform
        });
    }

    class PlayerProxyPair {
        public Transform physicsProxy;
        public Transform visualPlayer;
    }
}
