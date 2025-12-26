using UnityEngine;
using System.Collections;

public class ProjectileBehavior : MonoBehaviour
{
    [SerializeField] private TrailRenderer trail;
    private float speed;
    private const float DESPAWN_X = -20f;

    public void Initialize(float worldSpeed)
    {
        this.speed = worldSpeed * 1.5f;
        transform.rotation = Quaternion.Euler(0, 180, 0);
        VisualsInstaller.AttachProjectileLight(gameObject, Color.cyan, 2.0f, 3.0f);

        if (trail != null)
        {
            trail.Clear(); 
            trail.widthMultiplier = 0f; 
            StartCoroutine(AnimateTrail());
        }
    }

    private IEnumerator AnimateTrail()
    {
        float duration = 0.15f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            trail.widthMultiplier = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        trail.widthMultiplier = 1f;
    }

    private void OnDisable()
    {
        StopAllCoroutines(); 
    }

    private void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;

        if (transform.position.x < DESPAWN_X)
        {
            ObjectPooler.Instance.ReturnProjectile(this.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle") || other.CompareTag("Platform"))
        {
            ObjectPooler.Instance.ReturnProjectile(this.gameObject);
        }
    }
    
    public void HitByPlayer()
    {
        ObjectPooler.Instance.ReturnProjectile(this.gameObject);
    }
}