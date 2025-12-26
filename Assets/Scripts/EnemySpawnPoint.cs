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
                    GameObject minion = Instantiate(option.prefab, transform.position, transform.rotation, transform);
                    
                    // Dynamic Color Selection
                    Color lightColor = new Color(1f, 0.1f, 0.1f); 
                    string name = option.prefab.name.ToLower();
                    
                    if (name.Contains("purple") || name.Contains("violet") || name.Contains("shooter"))
                    {
                        lightColor = new Color(0.7f, 0.2f, 1f); // Purple
                    }
                    else if (name.Contains("blue") || name.Contains("cyan"))
                    {
                        lightColor = Color.cyan;
                    }
                    else if (name.Contains("static"))
                    {
                        lightColor = Color.white;
                    }

                    // Intensity 4.0, Radius 4.5
                    VisualsInstaller.AttachEnemyLight(minion, lightColor, 4.0f, 4.5f);
                }
                return;
            }
        }
    }
}