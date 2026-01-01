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
    [Tooltip("Animator to control")]
    [SerializeField] private Animator animator;

    public bool IsDead { get; private set; } = false;
    
    private Collider2D col;
    
    private WaitForSeconds waitStartDelay;
    private WaitForSeconds waitFireRate;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        waitStartDelay = new WaitForSeconds(startDelay);
        waitFireRate = new WaitForSeconds(fireRate);
    }

    private void OnEnable()
    {
        IsDead = false;
        if (col != null) col.enabled = true;
        StartCoroutine(FireRoutine());
    }

    public void TriggerDeath()
    {
        if (IsDead) return;
        IsDead = true;
        
        if (col != null) col.enabled = false;
        
        StopAllCoroutines();
        animator.SetTrigger("Die");
        Destroy(gameObject, 0.5f);
    }

    private IEnumerator FireRoutine()
    {
        yield return new WaitUntil(() => transform.position.x <= activationX);
        yield return waitStartDelay;

        while (!IsDead)
        {
            animator.SetTrigger("Shoot");
            yield return waitFireRate;
        }
    }
    
    public void FireProjectile()
    {
        if (IsDead || projectilePrefab == null) return;

        Vector3 spawnPos = transform.position + muzzleOffset;
        GameObject proj = ObjectPooler.Instance.GetProjectile(spawnPos, Quaternion.identity);
        
        if (proj.TryGetComponent(out ProjectileBehavior behavior))
            behavior.Initialize(GameManager.Instance.worldSpeed);
    }
}