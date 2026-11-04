using System.Collections.Generic;
using UnityEngine;

public class PlayerAndSpaceshipSync : MonoBehaviour
{
    [Header("Setari Interior (De pe aceasta nava)")]
    [Tooltip("Trage aici obiectul copil care contine toate collider-ele interioare ale navei")]
    [SerializeField] private Transform interiorCollidersRoot;

    [Tooltip("Obiect gol pus in InteriorColliders unde vrei sa apara jucatorii")]
    [SerializeField] private Transform localSpawnPoint;

    // Acestea se genereaza 100% automat din cod pentru fiecare nava!
    private Transform staticInteriorTransform;
    private Vector3 generatedSpawnPosition;
    private static int shipInstanceCounter = 0; // Ca sa nu se suprapuna daca ai mai multe nave in scena

    private class SyncedPlayer
    {
        public Transform physicsPlayer;
        public Transform visualDummy;
        public SpriteRenderer realSprite;
        public SpriteRenderer dummySprite;
    }

    private List<SyncedPlayer> syncedPlayers = new List<SyncedPlayer>();
    private PlayerJoinManager playerJoinManager;

    void Awake()
    {
        GenerateStaticProxyAutomatically();
        playerJoinManager = FindAnyObjectByType<PlayerJoinManager>();
        if (playerJoinManager != null)
        {
            playerJoinManager.OnPlayerJoined += HandlePlayerJoined;
        }
    }

    private void GenerateStaticProxyAutomatically()
    {
        shipInstanceCounter++;

        // 1. Calculam o pozitie departe sub harta (ex: Nava 1 la Y:-5000, Nava 2 la Y:-5500)
        Vector3 hiddenPosition = new Vector3(0, -5000f - (shipInstanceCounter * 500f), 0);

        // 2. Clonam automat doar collider-ele interioare ale acestei nave si le ducem jos
        GameObject staticCopy = Instantiate(interiorCollidersRoot.gameObject, hiddenPosition, Quaternion.identity);
        staticCopy.name = $"{gameObject.name}_AutoStaticInterior";
        staticInteriorTransform = staticCopy.transform;

        // 3. Calculam unde pica SpawnPoint-ul in copia statica
        if (localSpawnPoint != null)
        {
            Vector3 relativeSpawn = interiorCollidersRoot.InverseTransformPoint(localSpawnPoint.position);
            generatedSpawnPosition = staticInteriorTransform.TransformPoint(relativeSpawn);
        }
        else
        {
            generatedSpawnPosition = staticInteriorTransform.position;
        }

        // 4. DEZACTIVAM collider-ele interioare de pe nava care zboara!
        // (Nava din spatiu nu mai are nevoie de peretii interiori, doar de collider-ul exterior pt asteroizi)
        interiorCollidersRoot.gameObject.SetActive(false);
    }

    void Start()
    {
       
    }

    private void OnDestroy()
    {
        if (playerJoinManager != null)
        {
            playerJoinManager.OnPlayerJoined -= HandlePlayerJoined;
        }
        // Daca nava este distrusa in joc, stergem si copia ei statica
        if (staticInteriorTransform != null)
        {
            Destroy(staticInteriorTransform.gameObject);
        }
    }

    private void HandlePlayerJoined(GameObject playerObj)
    {
        // Mutam jucatorul in interiorul generat automat
        playerObj.transform.position = generatedSpawnPosition;

        if (playerObj.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Cream dublura vizuala direct pe aceasta nava (transform)
        GameObject dummyObj = new GameObject($"{playerObj.name}_VisualDummy");
        dummyObj.transform.SetParent(this.transform, false);

        SpriteRenderer realSr = playerObj.GetComponentInChildren<SpriteRenderer>();
        SpriteRenderer dummySr = dummyObj.AddComponent<SpriteRenderer>();

        if (realSr != null)
        {
            dummySr.sprite = realSr.sprite;
            dummySr.color = realSr.color;
            dummySr.sortingLayerID = realSr.sortingLayerID;
            dummySr.sortingOrder = realSr.sortingOrder;
            realSr.enabled = false;
        }

        syncedPlayers.Add(new SyncedPlayer
        {
            physicsPlayer = playerObj.transform,
            visualDummy = dummyObj.transform,
            realSprite = realSr,
            dummySprite = dummySr
        });
    }

    void LateUpdate()
    {
        for (int i = syncedPlayers.Count - 1; i >= 0; i--)
        {
            var pair = syncedPlayers[i];

            if (pair.physicsPlayer == null)
            {
                if (pair.visualDummy != null) Destroy(pair.visualDummy.gameObject);
                syncedPlayers.RemoveAt(i);
                continue;
            }

            pair.visualDummy.localPosition = staticInteriorTransform.InverseTransformPoint(pair.physicsPlayer.position);
            pair.visualDummy.localRotation = Quaternion.Inverse(staticInteriorTransform.rotation) * pair.physicsPlayer.rotation;
            pair.visualDummy.localScale = pair.physicsPlayer.localScale;

            if (pair.realSprite != null && pair.dummySprite != null)
            {
                pair.dummySprite.sprite = pair.realSprite.sprite;
                pair.dummySprite.flipX = pair.realSprite.flipX;
                pair.dummySprite.flipY = pair.realSprite.flipY;
            }
        }
    }
}