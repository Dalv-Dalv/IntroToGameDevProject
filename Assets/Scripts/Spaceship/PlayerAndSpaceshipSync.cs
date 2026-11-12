using System.Collections.Generic;
using UnityEngine;

public class PlayerAndSpaceshipSync : MonoBehaviour{
    [Header("Setari Interior (De pe aceasta nava)")]
    [Tooltip("Trage aici obiectul copil 'Interior' care contine platformele, peretii si consolele")]
    [SerializeField] private Transform interiorCollidersRoot;

    [Tooltip("Optional: Obiect gol pus in Interior unde vrei sa apara jucatorii")]
    [SerializeField] private Transform localSpawnPoint;

    private Transform staticInteriorTransform;
    private Vector3 generatedSpawnPosition;
    private static int shipInstanceCounter = 0;

    private class SyncedPlayer{
        public Transform physicsPlayer;
        public Transform visualDummy;
        public SpriteRenderer realSprite;
        public SpriteRenderer dummySprite;
    }

    private List<SyncedPlayer> syncedPlayers = new List<SyncedPlayer>();
    private PlayerJoinManager playerJoinManager;

    void Awake(){
        GenerateStaticProxyAutomatically();
        playerJoinManager = FindAnyObjectByType<PlayerJoinManager>();
        if (playerJoinManager != null){
            playerJoinManager.OnPlayerJoined += HandlePlayerJoined;
        }
    }

    private void GenerateStaticProxyAutomatically(){
        if (interiorCollidersRoot == null){
            Debug.LogError("Nu ai asignat 'Interior Colliders Root' pe PlayerAndSpaceshipSync!", this);
            return;
        }

        shipInstanceCounter++;

        // 1. Hidden position for the static ship (where players will physically be)
        Vector3 hiddenPosition = new Vector3(0, -5000f - (shipInstanceCounter * 500f), 0);

        // 2. Clone the interior for static physics
        GameObject staticCopy = Instantiate(interiorCollidersRoot.gameObject, hiddenPosition, Quaternion.identity);
        staticCopy.name = $"{gameObject.name}_StaticPhysicsInterior";
        staticInteriorTransform = staticCopy.transform;

        // 3. Calculate spawn position in static copy
        if (localSpawnPoint != null){
            Vector3 relativeSpawn = interiorCollidersRoot.InverseTransformPoint(localSpawnPoint.position);
            generatedSpawnPosition = staticInteriorTransform.TransformPoint(relativeSpawn);
        }
        else{
            generatedSpawnPosition = staticInteriorTransform.position;
        }

        // 4. Link static modules to their visual counterparts on the moving ship
        SpaceshipModule shipController = GetComponent<SpaceshipModule>();
        SpaceshipController[] visualModules = interiorCollidersRoot.GetComponentsInChildren<SpaceshipController>(true);
        SpaceshipController[] staticModules = staticCopy.GetComponentsInChildren<SpaceshipController>(true);

        for (int i = 0; i < staticModules.Length; i++){
            Transform visualCounterpart = (i < visualModules.Length) ? visualModules[i].transform : null;
            staticModules[i].Initialize(shipController, visualCounterpart);
        }

        // 5. Disable interactive modules and their triggers on the moving visual ship
        foreach (var visualModule in visualModules){
            visualModule.enabled = false;
            if (visualModule.TryGetComponent<Collider2D>(out var triggerCol)){
                triggerCol.enabled = false;
            }
        }

        // 6. Disable visuals on the static physics copy
        foreach (var sr in staticCopy.GetComponentsInChildren<SpriteRenderer>(true)){
            sr.enabled = false;
        }
    }

    private void OnDestroy(){
        if (playerJoinManager != null){
            playerJoinManager.OnPlayerJoined -= HandlePlayerJoined;
        }
        if (staticInteriorTransform != null){
            Destroy(staticInteriorTransform.gameObject);
        }
    }

    private void HandlePlayerJoined(GameObject playerObj){
      
        // Move the real player to the invisible static ship
        playerObj.transform.position = generatedSpawnPosition;

        if (playerObj.TryGetComponent<Rigidbody2D>(out var rb)){
            rb.linearVelocity = Vector2.zero;
        }

   
        // Create visual dummy on the flying ship (this.transform) and sync it with the real player in the static ship
        GameObject dummyObj = new GameObject($"{playerObj.name}_VisualDummy");
        dummyObj.transform.SetParent(this.transform, false);

        SpriteRenderer realSr = playerObj.GetComponentInChildren<SpriteRenderer>();
        SpriteRenderer dummySr = dummyObj.AddComponent<SpriteRenderer>();

        if (realSr != null){
            dummySr.sprite = realSr.sprite;
            dummySr.color = realSr.color;
            dummySr.sortingLayerID = realSr.sortingLayerID;
            dummySr.sortingOrder = realSr.sortingOrder;
            realSr.enabled = false; 
        }

        syncedPlayers.Add(new SyncedPlayer{
            physicsPlayer = playerObj.transform,
            visualDummy = dummyObj.transform,
            realSprite = realSr,
            dummySprite = dummySr
        });
    }

    void LateUpdate(){
        if (staticInteriorTransform == null) return;

        for (int i = syncedPlayers.Count - 1; i >= 0; i--){
            var pair = syncedPlayers[i];

            if (pair.physicsPlayer == null){
                if (pair.visualDummy != null) Destroy(pair.visualDummy.gameObject);
                syncedPlayers.RemoveAt(i);
                continue;
            }


       
            pair.visualDummy.localPosition = staticInteriorTransform.InverseTransformPoint(pair.physicsPlayer.position);
            pair.visualDummy.localRotation = Quaternion.Inverse(staticInteriorTransform.rotation) * pair.physicsPlayer.rotation;

            if (pair.realSprite != null && pair.dummySprite != null){
                pair.visualDummy.localScale = pair.realSprite.transform.lossyScale;
                pair.dummySprite.sprite = pair.realSprite.sprite;
                pair.dummySprite.flipX = pair.realSprite.flipX;
                pair.dummySprite.flipY = pair.realSprite.flipY;
            }
        }
    }
}