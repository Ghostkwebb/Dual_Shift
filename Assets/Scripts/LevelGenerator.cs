using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class LevelGenerator : MonoBehaviour
{
    [Tooltip("Chunks to spawn")]
    [SerializeField] private LevelChunk[] chunkPrefabs;
    [Tooltip("Point from which chunks spawn")]
    [SerializeField] private Transform spawnOrigin;
    [Tooltip("Width of each chunk")]
    [SerializeField] private float chunkWidth = 20f;
    [Tooltip("Number of chunks to keep on screen")]
    [SerializeField] private int chunksOnScreen = 3;
    
    [Header("Tutorial Chunks")]
    [Tooltip("Tutorial chunk for switch mechanic")]
    [SerializeField] private LevelChunk tutorialSwitchPrefab;
    [Tooltip("Tutorial chunk for attack mechanic")]
    [SerializeField] private LevelChunk tutorialAttackPrefab;
    [Tooltip("Tutorial chunk for end of tutorial")]
    [SerializeField] private LevelChunk tutorialEndPrefab;

    private float currentSpawnX;

    private LevelChunk lastChunk;
    private Dictionary<string, ObjectPool<LevelChunk>> pools;
    private Queue<LevelChunk> activeChunks = new Queue<LevelChunk>();
    private Camera mainCam;

    private void Awake()
    {
        pools = new Dictionary<string, ObjectPool<LevelChunk>>();

        foreach (var prefab in chunkPrefabs)
        {
            LevelChunk currentPrefab = prefab;

            var pool = new ObjectPool<LevelChunk>(
                createFunc: () => Instantiate(currentPrefab, transform),
                actionOnGet: chunk => chunk.gameObject.SetActive(true),
                actionOnRelease: chunk => chunk.gameObject.SetActive(false),
                actionOnDestroy: chunk => Destroy(chunk.gameObject),
                defaultCapacity: 5,
                maxSize: 10
            );

            pools.Add(prefab.name, pool);
        }
    }

    private void Start()
    {
        mainCam = Camera.main;
        currentSpawnX = spawnOrigin.position.x - chunkWidth;
        bool isTutorialDone = PlayerPrefs.GetInt("TutorialDone", 0) == 1;

        if (!isTutorialDone)
        {
            SpawnSpecificChunk(chunkPrefabs[0]); 
            SpawnSpecificChunk(chunkPrefabs[0]);
            SpawnSpecificChunk(tutorialSwitchPrefab); 
            SpawnSpecificChunk(tutorialAttackPrefab);
            SpawnSpecificChunk(tutorialEndPrefab);
            int chunksSpawned = 5;
            int remaining = (chunksOnScreen + 1) - chunksSpawned;
            
            for (int i = 0; i < remaining; i++) SpawnChunk();
        }
        else
        {
            for (int i = 0; i < chunksOnScreen + 1; i++)
            {
                if (i <= 1) 
                {
                    SpawnChunk(0);
                }
                else 
                {
                    SpawnChunk();
                }
            }
        }
    }
    
    private void SpawnSpecificChunk(LevelChunk prefab)
    {
        LevelChunk chunk = Instantiate(prefab, transform);
        

        float spawnX = lastChunk != null ? 
            lastChunk.transform.position.x + chunkWidth : 
            currentSpawnX;
        
        chunk.transform.position = new Vector3(spawnX, 0, 0);
        activeChunks.Enqueue(chunk);
        lastChunk = chunk;
    }

    private void Update()
    {
        if (activeChunks.Count == 0) return;
        
        // Despawn chunks that have gone fully offscreen (use while loop for high speeds)
        while (activeChunks.Count > 0 && activeChunks.Peek().transform.position.x < -(chunkWidth * 2))
        {
            DespawnChunk();
        }
        
        float screenRightEdge = mainCam != null ? 
            mainCam.transform.position.x + mainCam.orthographicSize * mainCam.aspect + chunkWidth :
            20f;
        
        while (activeChunks.Count > 0 && GetLastChunkRightEdge() < screenRightEdge + chunkWidth)
        {
            SpawnChunk();
        }
        
        while (activeChunks.Count < chunksOnScreen)
        {
            SpawnChunk();
        }
    }
    
    private float GetLastChunkRightEdge()
    {

        if (lastChunk == null) return 0;
        return lastChunk.transform.position.x + chunkWidth;
    }

    private void SpawnChunk(int? specificIndex = null)
    {
        LevelChunk prefab;

        if (specificIndex.HasValue)
        {
            prefab = chunkPrefabs[specificIndex.Value];
        }
        else
        {
            int randomIndex = Random.Range(0, chunkPrefabs.Length);
            prefab = chunkPrefabs[randomIndex];
        }
        
        LevelChunk chunk = pools[prefab.name].Get();
        
        // Position based on tracked lastChunk
        float spawnX = lastChunk != null ? 
            lastChunk.transform.position.x + chunkWidth : 
            currentSpawnX;
        
        chunk.transform.position = new Vector3(spawnX, 0, 0);
        activeChunks.Enqueue(chunk);
        lastChunk = chunk;
    }

    private void DespawnChunk()
    {
        LevelChunk chunk = activeChunks.Dequeue();
        string key = chunk.name.Replace("(Clone)", "");
        if (pools.ContainsKey(key))
        {
            pools[key].Release(chunk);
        }
        else
        {
            Destroy(chunk.gameObject);
        }
    }
}