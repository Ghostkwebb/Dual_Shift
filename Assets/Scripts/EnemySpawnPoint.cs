using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [System.Serializable]
    public class SpawnOption
    {
        public GameObject prefab;
        [Tooltip("Higher value = higher spawn chance relative to others")]
        [Range(0f, 100f)] public float weight = 10f;
    }

    [Tooltip("List of enemies with spawn weights")]
    [SerializeField] private SpawnOption[] enemyOptions;
    [Tooltip("Chance to spawn anything (0 = never, 1 = always)")]
    [Range(0f, 1f)] [SerializeField] private float globalSpawnChance = 0.8f;

    public void Spawn()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        if (Random.value > globalSpawnChance) return;

        float totalWeight = 0f;
        foreach (var option in enemyOptions)
            totalWeight += option.weight;

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var option in enemyOptions)
        {
            currentWeight += option.weight;
            if (randomValue <= currentWeight && option.prefab != null)
            {
                GameObject minion = Instantiate(option.prefab, transform.position, transform.rotation, transform);
                
                Color lightColor = new Color(1f, 0.1f, 0.1f);
                string name = option.prefab.name.ToLower();
                
                if (name.Contains("shooter") || name.Contains("purple"))
                    lightColor = new Color(0.6f, 0.1f, 0.9f);
                else if (name.Contains("cyan") || name.Contains("blue"))
                    lightColor = Color.cyan;
                else if (name.Contains("static"))
                    lightColor = Color.white;

                VisualsInstaller.AttachEnemyLight(minion, lightColor, 6.0f, 7.0f);
                return;
            }
        }
    }
}