using System.Collections;
using UnityEngine;

public class ShooterAI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("How close (X position) the enemy must be to start firing. 25 is roughly just off-screen.")]
    [SerializeField] private float activationX = 25.0f; 
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float fireRate = 1.5f;
    
    [Header("References")]
    [SerializeField] private SpriteRenderer render;

    private void OnEnable()
    {
        StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {

        yield return new WaitUntil(() => transform.position.x <= activationX);
        yield return new WaitForSeconds(startDelay);

        while (true) 
        {
            Color originalColor = render.color;
            render.color = Color.white;
            yield return new WaitForSeconds(0.5f); 
            render.color = originalColor;
            
            if (projectilePrefab != null)
            {
                GameObject proj = ObjectPooler.Instance.GetProjectile(transform.position, Quaternion.identity);
                
                if (proj.TryGetComponent(out ProjectileBehavior behavior))
                {
                    behavior.Initialize(GameManager.Instance.worldSpeed);
                }
            }
            
            yield return new WaitForSeconds(fireRate);
        }
    }
}