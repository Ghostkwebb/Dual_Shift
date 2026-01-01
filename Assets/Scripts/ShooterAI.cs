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
    private float nextFireTime;
    private float lastShotTime;
    private bool hasActivated;

    private void Awake()
    {
        if (GetComponents<ShooterAI>().Length > 1)
        {
            Destroy(this);
            return;
        }

        col = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        IsDead = false;
        hasActivated = false; // Reset activation state on enable to respect logic
        if (col != null) col.enabled = true;
    }

    private void Update()
    {
        if (IsDead) return;

        if (!hasActivated)
        {
            if (transform.position.x <= activationX)
            {
                hasActivated = true;
                nextFireTime = Time.time + startDelay;
            }
            return;
        }

        if (Time.time >= nextFireTime)
        {
            animator.SetTrigger("Shoot");
            StartCoroutine(DelayedFire(0.1f)); 
            nextFireTime = Time.time + fireRate;
        }
    }
    
    private IEnumerator DelayedFire(float delay)
    {
        yield return new WaitForSeconds(delay);
        FireProjectile();
    }

    public void TriggerDeath()
    {
        if (IsDead) return;
        IsDead = true;
        
        if (col != null) col.enabled = false;
        
        animator.SetTrigger("Die");
        Destroy(gameObject, 0.5f);
    }
    
    public void FireProjectile()
    {
        if (IsDead || projectilePrefab == null) return;
        
        if (Time.time < lastShotTime + 0.5f) return;
        
        lastShotTime = Time.time;

        Vector3 spawnPos = transform.position + muzzleOffset;
        GameObject proj = ObjectPooler.Instance.GetProjectile(spawnPos, Quaternion.identity);
        
        if (proj != null && proj.TryGetComponent(out ProjectileBehavior behavior))
        {
            behavior.Initialize(GameManager.Instance.worldSpeed);
        }
    }
}