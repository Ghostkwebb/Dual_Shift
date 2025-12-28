using UnityEngine;
using System.Collections;

public class ProjectileBehavior : MonoBehaviour
{
    [Tooltip("Trail effect (optional)")]
    [SerializeField] private TrailRenderer trail;
    [Tooltip("VFX spawned when destroyed by player")]
    [SerializeField] private GameObject deathVFXPrefab;
    
    private float speed;
    private const float DESPAWN_X = -20f;

    public void Initialize(float worldSpeed)
    {
        speed = worldSpeed * 1.5f;
        transform.rotation = Quaternion.Euler(0, 180, 0);
        VisualsInstaller.AttachProjectileLight(gameObject, new Color(0.6f, 0.1f, 0.9f), 2.5f, 3.5f);

        if (trail != null)
        {
            trail.Clear();
            trail.widthMultiplier = 0f;
            StartCoroutine(AnimateTrail());
        }
    }

    private IEnumerator AnimateTrail()
    {
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            trail.widthMultiplier = Mathf.Lerp(0f, 1f, elapsed / 0.15f);
            yield return null;
        }
        trail.widthMultiplier = 1f;
    }

    private void OnDisable() => StopAllCoroutines();

    private void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;
        if (transform.position.x < DESPAWN_X)
            ObjectPooler.Instance.ReturnProjectile(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle") || other.CompareTag("Platform"))
        {
            // Spawn death VFX when hitting obstacle
            if (deathVFXPrefab != null)
                Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            ObjectPooler.Instance.ReturnProjectile(gameObject);
        }
    }
    
    public void HitByPlayer()
    {
        if (deathVFXPrefab != null)
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
        ObjectPooler.Instance.ReturnProjectile(gameObject);
    }
}