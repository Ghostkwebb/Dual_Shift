using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] private LevelChunk[] chunkPrefabs;
    [SerializeField] private Transform spawnOrigin;
    [SerializeField] private float chunkWidth = 20f;
    [SerializeField] private int chunksOnScreen = 3;

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
        string key = chunk.name.Replace("(Clone)", ""); // Clean name for dictionary lookup
        pools[key].Release(chunk);
    }
}