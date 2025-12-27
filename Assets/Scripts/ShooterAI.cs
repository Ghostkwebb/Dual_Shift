using System.Collections;
using UnityEngine;

public class ShooterAI : MonoBehaviour
{
    [Tooltip("Projectile prefab to fire")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("X position where shooter starts firing")]
    [SerializeField] private float activationX = 25.0f;
    [Tooltip("Delay before first shot after activation")]
    [SerializeField] private float startDelay = 0.5f;
    [Tooltip("Time between shots")]
    [SerializeField] private float fireRate = 2.0f;
    [Tooltip("Offset from center for projectile spawn")]
    [SerializeField] private Vector3 muzzleOffset = new Vector3(-1.0f, 0.1f, 0f);
    [SerializeField] private Animator animator;

    private bool isDead = false;

    private void OnEnable()
    {
        isDead = false;
        GetComponent<Collider2D>().enabled = true;
        StartCoroutine(FireRoutine());
    }

    public void TriggerDeath()
    {
        isDead = true;
        StopAllCoroutines();
        animator.SetTrigger("Die");
        GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject, 0.5f);
    }

    private IEnumerator FireRoutine()
    {
        yield return new WaitUntil(() => transform.position.x <= activationX);
        yield return new WaitForSeconds(startDelay);

        while (!isDead)
        {
            animator.SetTrigger("Shoot");
            yield return new WaitForSeconds(fireRate);
        }
    }
    
    public void FireProjectile()
    {
        if (isDead || projectilePrefab == null) return;

        Vector3 spawnPos = transform.position + muzzleOffset;
        GameObject proj = ObjectPooler.Instance.GetProjectile(spawnPos, Quaternion.identity);
        
        if (proj.TryGetComponent(out ProjectileBehavior behavior))
            behavior.Initialize(GameManager.Instance.worldSpeed);
    }
}