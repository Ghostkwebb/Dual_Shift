using UnityEngine;

public class LevelChunk : MonoBehaviour
{
    private void OnEnable()
    {
        EnemySpawnPoint[] points = GetComponentsInChildren<EnemySpawnPoint>();
        foreach (var point in points)
        {
            point.Spawn();
        }
    }
}