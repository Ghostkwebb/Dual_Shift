using System.Collections;
using UnityEngine;

public class ShooterAI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float startDelay = 0.5f; // Reduced default (was 1.0)
    [SerializeField] private float fireRate = 1.5f;   // Time between shots

    [Header("References")]
    [SerializeField] private SpriteRenderer render;

    private void Start()
    {
        StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        // 1. Wait a tiny bit after spawning so it doesn't shoot INSTANTLY off-screen
        yield return new WaitForSeconds(startDelay);

        while (true) // Loop forever
        {
            // 2. Telegraph (Flash White)
            Color originalColor = render.color;
            render.color = Color.white;
            yield return new WaitForSeconds(0.5f); // 0.5s Warning
            render.color = originalColor;

            // 3. Fire
            if (projectilePrefab != null)
            {
                GameObject proj = ObjectPooler.Instance.GetProjectile(transform.position, Quaternion.identity);

                if (proj.TryGetComponent(out ProjectileBehavior behavior))
                {
                    behavior.Initialize(GameManager.Instance.worldSpeed);
                }
            }

            // 4. Wait for next shot
            yield return new WaitForSeconds(fireRate);
        }
    }
}