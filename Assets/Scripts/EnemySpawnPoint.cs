using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [System.Serializable]
    public class SpawnOption
    {
        public GameObject prefab;
        [Tooltip("Higher value = Higher chance to spawn compared to others.")]
        [Range(0f, 100f)] public float weight = 10f;
    }

    [Header("Configuration")]
    [Tooltip("List of enemies with individual spawn weights.")]
    [SerializeField] private SpawnOption[] enemyOptions;
    
    [Tooltip("Global chance to spawn ANYTHING (0 = Never, 1 = Always).")]
    [Range(0f, 1f)] [SerializeField] private float globalSpawnChance = 0.8f;

    public void Spawn()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        if (Random.value > globalSpawnChance) return;

        float totalWeight = 0f;
        foreach (var option in enemyOptions)
        {
            totalWeight += option.weight;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var option in enemyOptions)
        {
            currentWeight += option.weight;
            if (randomValue <= currentWeight)
            {
                if (option.prefab != null)
                {
                    Instantiate(option.prefab, transform.position, transform.rotation, transform);
                }
                return;
            }
        }
    }
}