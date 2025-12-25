using System.Collections;
using UnityEngine;

public class ShooterAI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float activationX = 25.0f; 
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float fireRate = 2.0f;
    [SerializeField] private Vector3 muzzleOffset = new Vector3(-1.0f, 0.1f, 0f);

    [Header("References")]
    [SerializeField] private SpriteRenderer render;
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
        if (isDead) return;

        if (projectilePrefab != null)
        {
            Vector3 spawnPos = transform.position + muzzleOffset;
            GameObject proj = ObjectPooler.Instance.GetProjectile(spawnPos, Quaternion.identity);
            
            if (proj.TryGetComponent(out ProjectileBehavior behavior))
            {
                behavior.Initialize(GameManager.Instance.worldSpeed);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + muzzleOffset, 0.1f);
    }
}