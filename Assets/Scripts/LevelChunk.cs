using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LevelChunk : MonoBehaviour
{
    // Static block glow color - cyan to match game aesthetic
    private static readonly Color obstacleGlowColor = new Color(0.3f, 0.8f, 1f);

    private void OnEnable()
    {
        foreach (var point in GetComponentsInChildren<EnemySpawnPoint>())
            point.Spawn();

        // Add lights to obstacles (StaticBlocks, etc.)
        AttachObstacleLights();
    }

    private void AttachObstacleLights()
    {
        foreach (var col in GetComponentsInChildren<Collider2D>())
        {
            if (col.CompareTag("Obstacle") || col.CompareTag("Platform"))
            {
                // Very low intensity to prevent bloom washing out color
                VisualsInstaller.AttachObstacleLight(col.gameObject, new Color(0f, 0.8f, 1f), 0.15f, 4f);
            }
        }
    }
}