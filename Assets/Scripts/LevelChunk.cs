using UnityEngine;

public class LevelChunk : MonoBehaviour
{
    private void OnEnable()
    {
        foreach (var point in GetComponentsInChildren<EnemySpawnPoint>())
            point.Spawn();
    }
}