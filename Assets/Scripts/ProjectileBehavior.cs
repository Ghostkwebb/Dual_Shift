using UnityEngine;

public class ProjectileBehavior : MonoBehaviour
{
    private float speed;
    private const float DESPAWN_X = -20f;

    public void Initialize(float worldSpeed)
    {
        this.speed = worldSpeed * 1.5f;
        transform.rotation = Quaternion.Euler(0, 180, 0);
    }

    private void Update()
    {
        // Move
        transform.position += Vector3.left * speed * Time.deltaTime;

        // Check Bounds
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
}