using UnityEngine;

public class LevelChunk : MonoBehaviour
{
    private void OnEnable()
    {
        foreach (var point in GetComponentsInChildren<EnemySpawnPoint>())
            point.Spawn();

        foreach (var col in GetComponentsInChildren<Collider2D>())
        {
            if (col.CompareTag("Obstacle") || col.CompareTag("Platform") || 
                col.name.Contains("Block") || col.name.Contains("Wall") || col.name.Contains("Ground"))
            {
                VisualsInstaller.AttachObstacleLight(col.gameObject, new Color(1f, 0.95f, 0.9f), 5.0f, 6.0f);
            }
        }
    }
}