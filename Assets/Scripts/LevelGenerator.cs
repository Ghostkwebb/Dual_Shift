using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] private LevelChunk[] chunkPrefabs;
    [SerializeField] private Transform spawnOrigin;
    [SerializeField] private float chunkWidth = 20f;
    [SerializeField] private int chunksOnScreen = 3;
    
    [Header("Tutorial Chunks")]
    [SerializeField] private LevelChunk tutorialSwitchPrefab;
    [SerializeField] private LevelChunk tutorialAttackPrefab;
    [SerializeField] private LevelChunk tutorialEndPrefab;

    private float currentSpawnX;
    private Dictionary<string, ObjectPool<LevelChunk>> pools;
    private Queue<LevelChunk> activeChunks = new Queue<LevelChunk>();

    private void Awake()
    {
        pools = new Dictionary<string, ObjectPool<LevelChunk>>();

        foreach (var prefab in chunkPrefabs)
        {
            // Capture prefab in local scope for the lambda
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
        if (activeChunks.Count > 0)
        {
            LevelChunk lastChunk = activeChunks.ToArray()[activeChunks.Count - 1];
            chunk.transform.position = new Vector3(lastChunk.transform.position.x + chunkWidth, 0, 0);
        }
        else
        {
            chunk.transform.position = new Vector3(currentSpawnX, 0, 0);
        }
        activeChunks.Enqueue(chunk);
    }

    private void Update()
    {
        if (activeChunks.Peek().transform.position.x < -(chunkWidth * 2))
        {
            DespawnChunk();
            SpawnChunk();
        }
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
        chunk.transform.position = new Vector3(currentSpawnX, 0, 0);
        
        if (activeChunks.Count > 0)
        {
            LevelChunk lastChunk = activeChunks.ToArray()[activeChunks.Count - 1];
            chunk.transform.position = new Vector3(lastChunk.transform.position.x + chunkWidth, 0, 0);
        }
        
        activeChunks.Enqueue(chunk);
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