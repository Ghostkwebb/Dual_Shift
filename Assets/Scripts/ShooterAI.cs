using System.Collections;
using UnityEngine;

public class ShooterAI : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float startDelay = 1.0f; // Wait before firing
    [SerializeField] private SpriteRenderer render;

    private void Start()
    {
        StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        // Wait for the enemy to enter the screen roughly
        yield return new WaitForSeconds(startDelay);

        // Telegraph (Flash Bright)
        Color originalColor = render.color;
        render.color = Color.white;
        yield return new WaitForSeconds(0.5f);
        render.color = originalColor;

        // Fire
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        // Pass the current world speed to the projectile
        if (proj.TryGetComponent(out ProjectileBehavior behavior))
        {
            behavior.Initialize(GameManager.Instance.worldSpeed);
        }
    }
}