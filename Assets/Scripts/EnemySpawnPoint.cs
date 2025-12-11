using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("List of enemies that can appear here")]
    [SerializeField] private GameObject[] enemyPrefabs;
    
    [Tooltip("0 = Never, 1 = Always")]
    [Range(0f, 1f)] [SerializeField] private float spawnChance = 1.0f;

    public void Spawn()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        
        if (Random.value > spawnChance) return;
        
        if (enemyPrefabs.Length > 0)
        {
            int index = Random.Range(0, enemyPrefabs.Length);
            GameObject prefab = enemyPrefabs[index];
            
            Instantiate(prefab, transform.position, transform.rotation, transform);
        }
    }
}