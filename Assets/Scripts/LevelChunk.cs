using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LevelChunk : MonoBehaviour
{
    private static readonly Color obstacleGlowColor = new Color(0.3f, 0.8f, 1f);
    
    private EnemySpawnPoint[] cachedSpawnPoints;
    private Collider2D[] cachedColliders;
    private bool lightsAttached = false;

    private void Awake()
    {

        cachedSpawnPoints = GetComponentsInChildren<EnemySpawnPoint>();
        cachedColliders = GetComponentsInChildren<Collider2D>();
    }


    private void OnEnable()
    {
        foreach (var point in cachedSpawnPoints)
            if (point != null) point.Spawn();

        if (!lightsAttached)
        {
            AttachObstacleLights();
            lightsAttached = true;
        }
    }

    private void AttachObstacleLights()
    {
        foreach (var col in cachedColliders)
        {
            if (col != null && (col.CompareTag("Obstacle") || col.CompareTag("Platform")))
            {

                VisualsInstaller.AttachObstacleLight(col.gameObject, new Color(0f, 0.8f, 1f), 0.15f, 4f);
            }
        }
    }
}