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

        // Auto-light Obstacles and Platforms
        // Using Collider2D as a filter (since obstacles usually have physics)
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (var col in cols)
        {
            // Check Tags OR Names (in case tags are missing)
            if (col.CompareTag("Obstacle") || col.CompareTag("Platform") || 
                col.name.Contains("Block") || col.name.Contains("Wall") || col.name.Contains("Ground"))
            {
                // Attach a soft white light
                // Intensity 2.0, Radius 4.0
                VisualsInstaller.AttachObstacleLight(col.gameObject, Color.white, 2.0f, 4.0f);
            }
        }
    }
}