using System.Collections.Generic;
using UnityEngine;

public class PlayerAndSpaceshipSync : MonoBehaviour {
    [Header("Setari Interior")]
    [SerializeField] private Transform interiorRoot;
    [SerializeField] private Transform localSpawnPoint;

    [Header("Layere")]
    [Tooltip("Layer-ul atribuit corpului fizic din camera statica (ex: ProxyShaceship)")]
    [SerializeField] private string proxyPlayerLayerName = "ProxySpaceship";

    private Transform staticPhysicsRoom;
    private Vector3 staticSpawnPosition;
    private Vector3 interiorLocalOffset;
    private Quaternion interiorLocalRotation;
    private static int shipInstanceCounter = 0;

    private class PlayerProxyPair {
        public Transform visualPlayer;
        public Transform physicsProxy;
    }

    private List<PlayerProxyPair> activePlayers = new List<PlayerProxyPair>();
    private PlayerJoinManager playerJoinManager;

    void Awake() {
        BuildStaticCollisionRoom();

        playerJoinManager = FindAnyObjectByType<PlayerJoinManager>();
        if (playerJoinManager != null) {
            playerJoinManager.OnPlayerJoined += HandlePlayerJoined;
        }
    }

    private void BuildStaticCollisionRoom() {
        if (interiorRoot == null) return;

        shipInstanceCounter++;
        interiorLocalOffset = transform.InverseTransformPoint(interiorRoot.position);
        interiorLocalRotation = Quaternion.Inverse(transform.rotation) * interiorRoot.rotation;

        Vector3 hiddenPosition = new Vector3(0, -5000f - (shipInstanceCounter * 500f), 0);

        // Clonam interiorul (pereții au deja layer-ul InnerSpaceship)
        GameObject staticRoomObj = Instantiate(interiorRoot.gameObject, hiddenPosition, Quaternion.identity);
        staticRoomObj.name = $"{gameObject.name}_PhysicsWallsOnly";
        staticPhysicsRoom = staticRoomObj.transform;

        if (localSpawnPoint != null) {
            Vector3 relSpawn = interiorRoot.InverseTransformPoint(localSpawnPoint.position);
            staticSpawnPosition = staticPhysicsRoom.TransformPoint(relSpawn);
        }
        else {
            staticSpawnPosition = staticPhysicsRoom.position;
        }

        // Stergem doar vizualul si scripturile de console din copia statica
        foreach (var sr in staticRoomObj.GetComponentsInChildren<SpriteRenderer>(true)) Destroy(sr);
        foreach (var module in staticRoomObj.GetComponentsInChildren<SpaceshipController>(true)) Destroy(module);
        foreach (var col in staticRoomObj.GetComponentsInChildren<Collider2D>(true)) {
            if (col.isTrigger) Destroy(col);
        }
    }

    public void HandlePlayerJoined(GameObject playerObj) {
        if (activePlayers.Exists(p => p.visualPlayer == playerObj.transform)) return;

        // 1. Cream Proxy-ul in camera statica pe layer-ul ProxyShaceship
        GameObject proxyObj = new GameObject($"{playerObj.name}_PhysicsProxy");
        proxyObj.transform.position = staticSpawnPosition;
        proxyObj.layer = LayerMask.NameToLayer(proxyPlayerLayerName);

        // 2. Copiem Rigidbody2D pe proxy, iar jucatorul de pe nava devine Kinematic
        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
        Rigidbody2D proxyRb = proxyObj.AddComponent<Rigidbody2D>();
        if (playerRb != null) {
            proxyRb.mass = playerRb.mass;
            proxyRb.linearDamping = playerRb.linearDamping;
            proxyRb.angularDamping = playerRb.angularDamping;
            proxyRb.gravityScale = playerRb.gravityScale;
            proxyRb.constraints = playerRb.constraints;
            proxyRb.interpolation = RigidbodyInterpolation2D.Interpolate;

            playerRb.bodyType = RigidbodyType2D.Kinematic;
            playerRb.linearVelocity = Vector2.zero;
        }

        // 3. Copiem CircleCollider2D-ul pe proxy (cel de pe player ramane activ pe layer-ul Player, dar nu colizioneaza cu nava datorita matricei!)
        CircleCollider2D playerCircle = playerObj.GetComponent<CircleCollider2D>();
        if (playerCircle != null) {
            CircleCollider2D proxyCircle = proxyObj.AddComponent<CircleCollider2D>();
            proxyCircle.radius = playerCircle.radius;
            proxyCircle.offset = playerCircle.offset;
            proxyCircle.sharedMaterial = playerCircle.sharedMaterial;
        }

        // 4. Atasam jucatorul la nava principala
        playerObj.transform.SetParent(this.transform, false);

        // 5. Conectam PlayerMovement la proxyRb
        if (playerObj.TryGetComponent<PlayerMovement>(out var movement)) {
            movement.SetPhysicsRigidbody(proxyRb);
        }

        activePlayers.Add(new PlayerProxyPair {
            visualPlayer = playerObj.transform,
            physicsProxy = proxyObj.transform
        });
    }

    void LateUpdate() {
        if (staticPhysicsRoom == null) return;

        for (int i = activePlayers.Count - 1; i >= 0; i--) {
            var pair = activePlayers[i];
            if (pair.visualPlayer == null) {
                if (pair.physicsProxy != null) Destroy(pair.physicsProxy.gameObject);
                activePlayers.RemoveAt(i);
                continue;
            }

            Vector3 relPos = staticPhysicsRoom.InverseTransformPoint(pair.physicsProxy.position);
            pair.visualPlayer.localPosition = interiorLocalOffset + (interiorLocalRotation * relPos);
            pair.visualPlayer.localRotation = interiorLocalRotation;
        }
    }

    private void OnDestroy() {
        if (playerJoinManager != null) playerJoinManager.OnPlayerJoined -= HandlePlayerJoined;
        if (staticPhysicsRoom != null) Destroy(staticPhysicsRoom.gameObject);
        foreach (var pair in activePlayers) {
            if (pair.physicsProxy != null) Destroy(pair.physicsProxy.gameObject);
        }
    }
#if UNITY_EDITOR
    private void OnValidate() {
        if (LayerMask.NameToLayer(proxyPlayerLayerName) == -1) {
            Debug.LogWarning($"[PlayerAndSpaceshipSync] Layer '{proxyPlayerLayerName}' dosen t exist", this);
        }
    }
#endif
}